using Matbiz.Web.Data;
using Matbiz.Web.Modules.Articles.Models;
using Matbiz.Web.Modules.Articles.Services;
using Matbiz.Web.Modules.Customers.Services;
using Matbiz.Web.Modules.Documents.Models;
using Matbiz.Web.Shared;
using Microsoft.EntityFrameworkCore;

namespace Matbiz.Web.Modules.Documents.Services;

public class DocumentService(
    ApplicationDbContext db,
    NumberRangeService numbers,
    CustomerService customers,
    CompanyService companies,
    ICurrentUserAccessor currentUser)
{
    public Task<List<Document>> ListAsync(CancellationToken ct = default) =>
        db.Documents.AsNoTracking()
            .Include(d => d.Customer)
            .Include(d => d.Company)
            .OrderByDescending(d => d.DocumentDate)
            .ThenByDescending(d => d.CreatedAt)
            .ToListAsync(ct);

    public Task<Document?> GetAsync(Guid id, CancellationToken ct = default) =>
        db.Documents
            .Include(d => d.Customer)
            .Include(d => d.Company)
            .Include(d => d.SourceDocument)
            .Include(d => d.Positions.OrderBy(p => p.Position))
                .ThenInclude(p => p.Article)
            .FirstOrDefaultAsync(d => d.Id == id, ct);

    private static string NumberRangeKey(DocumentType t) => t switch
    {
        DocumentType.Offer => "Offer",
        DocumentType.Order => "Order",
        DocumentType.Invoice => "Invoice",
        DocumentType.CreditNote => "CreditNote",
        _ => t.ToString()
    };

    /// <summary>Erstellt einen neuen Beleg als Entwurf mit nächster Nummer aus dem Kreis.</summary>
    public async Task<Document> CreateDraftAsync(DocumentType type, Guid? customerId, Guid? companyId, CancellationToken ct = default)
    {
        var ctx = await currentUser.GetAsync();
        var doc = new Document
        {
            Type = type,
            Status = DocumentStatus.Draft,
            DocumentDate = DateTime.UtcNow,
            CreatedByUserId = ctx.UserId ?? string.Empty
        };

        await SnapshotRecipientAsync(doc, customerId, companyId, ct);

        doc.Number = await numbers.NextAsync(NumberRangeKey(type), ct);
        db.Documents.Add(doc);
        await db.SaveChangesAsync(ct);
        return doc;
    }

    public async Task UpdateHeaderAsync(Document doc, CancellationToken ct = default)
    {
        doc.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task SetStatusAsync(Guid id, DocumentStatus status, CancellationToken ct = default)
    {
        var d = await db.Documents.FindAsync([id], ct);
        if (d is null) return;
        // Stornierte Belege sind read-only — Status-Change blockiert (Audit-Compliance).
        if (d.Status == DocumentStatus.Cancelled && status != DocumentStatus.Cancelled)
            throw new InvalidOperationException("Stornierte Belege können nicht reaktiviert werden.");
        d.Status = status;
        d.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var d = await db.Documents.FindAsync([id], ct);
        if (d is null) return;
        db.Documents.Remove(d);
        await db.SaveChangesAsync(ct);
    }

    // --- Positionen --------------------------------------------------------

    public async Task<DocumentPosition> AddPositionAsync(Guid docId, Guid? articleId, CancellationToken ct = default)
    {
        var doc = await db.Documents
            .Include(d => d.Positions)
            .FirstOrDefaultAsync(d => d.Id == docId, ct)
            ?? throw new InvalidOperationException("Beleg nicht gefunden.");
        if (doc.Status == DocumentStatus.Cancelled)
            throw new InvalidOperationException("Stornierte Belege sind read-only.");
        var nextPos = (doc.Positions.Count == 0 ? 0 : doc.Positions.Max(p => p.Position)) + 1;

        var p = new DocumentPosition { DocumentId = docId, Position = nextPos };

        if (articleId is Guid aid)
        {
            var a = await db.Articles.Include(x => x.TaxRate).FirstOrDefaultAsync(x => x.Id == aid, ct);
            if (a is not null) ApplyArticleSnapshot(p, a);
        }
        else
        {
            // Default-Position bekommt den Default-Steuersatz
            var defTax = await db.TaxRates.Where(t => t.IsDefault).Select(t => t.Percent).FirstOrDefaultAsync(ct);
            p.TaxRatePercent = defTax;
        }
        p.Recalculate();
        db.DocumentPositions.Add(p);
        doc.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await RecalculateTotalsAsync(docId, ct);
        return p;
    }

    private static void ApplyArticleSnapshot(DocumentPosition p, Article a)
    {
        p.ArticleId = a.Id;
        p.ArticleNumber = a.Number;
        p.Description = a.Name;
        p.Unit = a.Unit;
        p.NetPrice = a.NetPrice;
        p.TaxRatePercent = a.TaxRate?.Percent ?? 0m;
        p.VatCategoryCode = p.TaxRatePercent > 0m ? "S" : "Z";
    }

    public async Task UpdatePositionAsync(DocumentPosition input, CancellationToken ct = default)
    {
        var p = await db.DocumentPositions.Include(x => x.Document).FirstOrDefaultAsync(x => x.Id == input.Id, ct);
        if (p is null) return;
        if (p.Document.Status == DocumentStatus.Cancelled)
            throw new InvalidOperationException("Stornierte Belege sind read-only.");
        p.Description = input.Description;
        p.Unit = input.Unit;
        p.Quantity = input.Quantity;
        p.NetPrice = input.NetPrice;
        p.DiscountPercent = input.DiscountPercent;
        p.TaxRatePercent = input.TaxRatePercent;
        p.Recalculate();
        await db.SaveChangesAsync(ct);
        await RecalculateTotalsAsync(p.DocumentId, ct);
    }

    public async Task DeletePositionAsync(Guid positionId, CancellationToken ct = default)
    {
        var p = await db.DocumentPositions.FindAsync([positionId], ct);
        if (p is null) return;
        var docId = p.DocumentId;
        db.DocumentPositions.Remove(p);
        await db.SaveChangesAsync(ct);
        await RecalculateTotalsAsync(docId, ct);
    }

    /// <summary>Aggregiert Net/Tax/Gross aus allen Positionen und persistiert es am Beleg.</summary>
    public async Task RecalculateTotalsAsync(Guid docId, CancellationToken ct = default)
    {
        var doc = await db.Documents.Include(d => d.Positions).FirstOrDefaultAsync(d => d.Id == docId, ct);
        if (doc is null) return;
        doc.NetTotal = doc.Positions.Sum(p => p.NetTotal);
        doc.TaxTotal = doc.Positions.Sum(p => p.TaxTotal);
        doc.GrossTotal = doc.NetTotal + doc.TaxTotal;
        doc.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    // --- Vorgangskette -----------------------------------------------------

    /// <summary>Erlaubte Folge-Typen: Offer→Order, Order/Offer→Invoice, Invoice→CreditNote.</summary>
    public static DocumentType[] NextTypes(DocumentType t) => t switch
    {
        DocumentType.Offer      => new[] { DocumentType.Order, DocumentType.Invoice },
        DocumentType.Order      => new[] { DocumentType.Invoice },
        DocumentType.Invoice    => new[] { DocumentType.CreditNote },
        _ => Array.Empty<DocumentType>()
    };

    public async Task<Document> ConvertAsync(Guid sourceId, DocumentType targetType, CancellationToken ct = default)
    {
        var src = await db.Documents.Include(d => d.Positions).FirstOrDefaultAsync(d => d.Id == sourceId, ct)
            ?? throw new InvalidOperationException("Quell-Beleg nicht gefunden.");
        if (!NextTypes(src.Type).Contains(targetType))
            throw new InvalidOperationException($"Umwandlung {src.Type} → {targetType} nicht erlaubt.");

        var ctx = await currentUser.GetAsync();
        var dst = new Document
        {
            Type = targetType,
            Status = DocumentStatus.Draft,
            DocumentDate = DateTime.UtcNow,
            CustomerId = src.CustomerId,
            CompanyId = src.CompanyId,
            CustomerNameSnapshot = src.CustomerNameSnapshot,
            CustomerAddressSnapshot = src.CustomerAddressSnapshot,
            CustomerEmailSnapshot = src.CustomerEmailSnapshot,
            SourceDocumentId = src.Id,
            HeaderText = src.HeaderText,
            FooterText = src.FooterText,
            PaymentTerms = src.PaymentTerms,
            CreatedByUserId = ctx.UserId ?? string.Empty
        };
        dst.Number = await numbers.NextAsync(NumberRangeKey(targetType), ct);

        foreach (var p in src.Positions.OrderBy(p => p.Position))
        {
            dst.Positions.Add(new DocumentPosition
            {
                Position = p.Position,
                ArticleId = p.ArticleId,
                ArticleNumber = p.ArticleNumber,
                Description = p.Description,
                Unit = p.Unit,
                Quantity = p.Quantity,
                NetPrice = p.NetPrice,
                DiscountPercent = p.DiscountPercent,
                TaxRatePercent = p.TaxRatePercent,
                NetTotal = p.NetTotal,
                TaxTotal = p.TaxTotal,
                GrossTotal = p.GrossTotal
            });
        }
        dst.NetTotal = src.NetTotal;
        dst.TaxTotal = src.TaxTotal;
        dst.GrossTotal = src.GrossTotal;

        db.Documents.Add(dst);
        await db.SaveChangesAsync(ct);
        return dst;
    }

    // --- Helpers -----------------------------------------------------------

    private async Task SnapshotRecipientAsync(Document doc, Guid? customerId, Guid? companyId, CancellationToken ct)
    {
        if (customerId is Guid cid)
        {
            var c = await customers.GetAsync(cid, ct);
            if (c is not null)
            {
                doc.CustomerId = cid;
                doc.CompanyId = c.CompanyId;
                doc.CustomerNameSnapshot = c.Name;
                doc.CustomerEmailSnapshot = c.Email;
                doc.CustomerAddressSnapshot = JoinAddress(c.EffectiveCompanyName, c.Street, c.PostalCode, c.City, c.Country);
                // USt-ID aus Customer ODER aus zugeordneter Company (Company hat Vorrang)
                doc.BuyerVatIdSnapshot = c.Company?.VatId ?? c.VatId;
                doc.BuyerReference = c.Company?.BuyerReference;
                return;
            }
        }
        if (companyId is Guid coid)
        {
            var co = await companies.GetAsync(coid, ct);
            if (co is not null)
            {
                doc.CompanyId = coid;
                doc.CustomerNameSnapshot = co.Name;
                doc.CustomerEmailSnapshot = co.Email;
                doc.CustomerAddressSnapshot = JoinAddress(null, co.Street, co.PostalCode, co.City, co.Country);
                doc.BuyerVatIdSnapshot = co.VatId;
                doc.BuyerReference = co.BuyerReference;
            }
        }
    }

    private static string? JoinAddress(string? company, string? street, string? plz, string? city, string? country)
    {
        var lines = new List<string>();
        if (!string.IsNullOrWhiteSpace(company)) lines.Add(company!);
        if (!string.IsNullOrWhiteSpace(street))  lines.Add(street!);
        var plzOrt = $"{plz} {city}".Trim();
        if (!string.IsNullOrWhiteSpace(plzOrt))  lines.Add(plzOrt);
        if (!string.IsNullOrWhiteSpace(country)) lines.Add(country!);
        return lines.Count == 0 ? null : string.Join("\n", lines);
    }

    // --- Labels für UI -----------------------------------------------------

    public static string TypeLabel(DocumentType t) => t switch
    {
        DocumentType.Offer => "Angebot",
        DocumentType.Order => "Auftrag",
        DocumentType.Invoice => "Rechnung",
        DocumentType.CreditNote => "Gutschrift",
        _ => t.ToString()
    };

    public static string StatusLabel(DocumentStatus s) => s switch
    {
        DocumentStatus.Draft => "Entwurf",
        DocumentStatus.Sent => "Versendet",
        DocumentStatus.Accepted => "Angenommen",
        DocumentStatus.PartiallyPaid => "Teilweise bezahlt",
        DocumentStatus.Paid => "Bezahlt",
        DocumentStatus.Cancelled => "Storniert",
        _ => s.ToString()
    };

    public static string StatusBadgeClass(DocumentStatus s) => s switch
    {
        DocumentStatus.Draft => "bg-secondary-subtle text-secondary",
        DocumentStatus.Sent => "bg-info-subtle text-info",
        DocumentStatus.Accepted => "bg-primary-subtle text-primary",
        DocumentStatus.Paid => "bg-success-subtle text-success",
        DocumentStatus.PartiallyPaid => "bg-warning-subtle text-warning",
        DocumentStatus.Cancelled => "bg-danger-subtle text-danger",
        _ => "bg-secondary-subtle text-secondary"
    };
}
