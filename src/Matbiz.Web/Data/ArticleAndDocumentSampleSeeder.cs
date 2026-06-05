using Matbiz.Web.Modules.Articles.Models;
using Matbiz.Web.Modules.Articles.Services;
using Matbiz.Web.Modules.Documents.Models;
using Matbiz.Web.Modules.Documents.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Matbiz.Web.Data;

/// <summary>
/// Seedet ein paar Artikel und Belege (verschiedene Typen + Vorgangskette)
/// zum Spielen. Idempotent: läuft nicht wenn schon Artikel existieren.
/// </summary>
public static class ArticleAndDocumentSampleSeeder
{
    public static async Task SeedAsync(IServiceProvider sp, IConfiguration cfg, ILogger logger, CancellationToken ct = default)
    {
        if (!cfg.GetValue<bool>("Matbiz:SeedSampleData")) return;

        var db = sp.GetRequiredService<ApplicationDbContext>();
        if (await db.Articles.AnyAsync(ct))
        {
            logger.LogInformation("Article sample data: already present, skipping.");
            return;
        }

        var taxFull = await db.TaxRates.FirstOrDefaultAsync(t => t.Percent == 19m, ct);
        var taxRed  = await db.TaxRates.FirstOrDefaultAsync(t => t.Percent == 7m,  ct);
        if (taxFull is null || taxRed is null)
        {
            logger.LogWarning("Article sample data: TaxRates not seeded yet, skipping.");
            return;
        }

        var numberRanges = sp.GetRequiredService<NumberRangeService>();

        // === Artikel anlegen ===
        var articles = new[]
        {
            new Article { Name = "Beratungsstunde", Description = "Strategie & Konzept, pro Stunde.",
                          Type = ArticleType.Service, Unit = "h",
                          NetPrice = 120m, TaxRateId = taxFull.Id, Category = "Dienstleistung", SortOrder = 1 },
            new Article { Name = "Tagessatz Beratung", Description = "8 Std. Beratung am Stück.",
                          Type = ArticleType.Service, Unit = "Tag",
                          NetPrice = 880m, TaxRateId = taxFull.Id, Category = "Dienstleistung", SortOrder = 2 },
            new Article { Name = "Software-Lizenz Basis", Description = "Jahres-Lizenz pro Arbeitsplatz.",
                          Type = ArticleType.Product, Unit = "Stück",
                          NetPrice = 480m, PurchasePrice = 120m, TaxRateId = taxFull.Id, Category = "Software", SortOrder = 3 },
            new Article { Name = "Software-Lizenz Pro", Description = "Jahres-Lizenz pro Arbeitsplatz, alle Module.",
                          Type = ArticleType.Product, Unit = "Stück",
                          NetPrice = 960m, PurchasePrice = 240m, TaxRateId = taxFull.Id, Category = "Software", SortOrder = 4 },
            new Article { Name = "Schulung Vor-Ort", Description = "Eintägige Inhouse-Schulung, bis 8 TN.",
                          Type = ArticleType.Service, Unit = "Pauschal",
                          NetPrice = 1600m, TaxRateId = taxFull.Id, Category = "Schulung", SortOrder = 5 },
            new Article { Name = "Druckwerk Handbuch", Description = "Gebundene Anwender-Doku, A4.",
                          Type = ArticleType.Product, Unit = "Stück",
                          NetPrice = 24m, PurchasePrice = 8m, TaxRateId = taxRed.Id, Category = "Drucksachen", SortOrder = 6 },
            new Article { Name = "Express-Versand", Description = "Aufschlag für Same-Day-Lieferung.",
                          Type = ArticleType.Service, Unit = "Pauschal",
                          NetPrice = 35m, TaxRateId = taxFull.Id, Category = "Versand", SortOrder = 7 },
            new Article { Name = "Wartungspauschale Monat", Description = "Monatlicher Wartungsvertrag.",
                          Type = ArticleType.Service, Unit = "Pauschal",
                          NetPrice = 290m, TaxRateId = taxFull.Id, Category = "Wartung", IsActive = true, SortOrder = 8 },
        };
        foreach (var a in articles)
            a.Number = await numberRanges.NextAsync("Article", ct);

        db.Articles.AddRange(articles);
        await db.SaveChangesAsync(ct);

        // === Belege anlegen ===
        var customer = await db.Customers.OrderBy(c => c.Name).FirstOrDefaultAsync(ct);
        if (customer is null) { logger.LogWarning("Article seeder: no customer to anchor documents to."); return; }

        var adminId = await db.Users
            .Where(u => u.UserName == "admin@matbiz.local")
            .Select(u => u.Id).FirstOrDefaultAsync(ct) ?? "";

        // Helper: Beleg + 2-3 Positionen + Summen
        async Task<Document> CreateAsync(DocumentType type, DocumentStatus status, DateTime date,
            Guid? sourceId, (Article art, decimal qty, decimal? discount)[] lines, string headerKey)
        {
            var addr = $"{customer.Name}\n{customer.EffectiveCompanyName}\n{customer.Street}\n{customer.PostalCode} {customer.City}\n{customer.Country}";
            var doc = new Document
            {
                Type = type,
                Status = status,
                DocumentDate = date,
                DueDate = type == DocumentType.Invoice ? date.AddDays(14) : date.AddDays(30),
                CustomerId = customer.Id,
                CompanyId = customer.CompanyId,
                CustomerNameSnapshot = customer.Name,
                CustomerEmailSnapshot = customer.Email,
                CustomerAddressSnapshot = addr,
                SourceDocumentId = sourceId,
                HeaderText = headerKey switch
                {
                    "offer" => "Vielen Dank für Ihre Anfrage. Hiermit unterbreiten wir Ihnen folgendes Angebot:",
                    "invoice" => "Vielen Dank für Ihren Auftrag. Hiermit stellen wir Ihnen folgende Leistungen in Rechnung:",
                    _ => null
                },
                PaymentTerms = type == DocumentType.Invoice ? "Zahlbar binnen 14 Tagen netto ohne Abzug." : null,
                CreatedByUserId = adminId,
                Number = await numberRanges.NextAsync(type switch
                {
                    DocumentType.Offer => "Offer",
                    DocumentType.Order => "Order",
                    DocumentType.Invoice => "Invoice",
                    DocumentType.CreditNote => "CreditNote",
                    _ => "Offer"
                }, ct)
            };

            int posIdx = 1;
            foreach (var (art, qty, discount) in lines)
            {
                var pos = new DocumentPosition
                {
                    Position = posIdx++,
                    ArticleId = art.Id,
                    ArticleNumber = art.Number,
                    Description = art.Name,
                    Unit = art.Unit,
                    Quantity = qty,
                    NetPrice = art.NetPrice,
                    DiscountPercent = discount ?? 0m,
                    TaxRatePercent = taxFull.Id == art.TaxRateId ? taxFull.Percent : taxRed.Percent
                };
                pos.Recalculate();
                doc.Positions.Add(pos);
            }
            doc.NetTotal = doc.Positions.Sum(p => p.NetTotal);
            doc.TaxTotal = doc.Positions.Sum(p => p.TaxTotal);
            doc.GrossTotal = doc.NetTotal + doc.TaxTotal;
            db.Documents.Add(doc);
            await db.SaveChangesAsync(ct);
            return doc;
        }

        // 1) Angebot — Entwurf
        await CreateAsync(DocumentType.Offer, DocumentStatus.Draft, DateTime.UtcNow.AddDays(-3),
            null,
            new[]
            {
                (articles[2], 5m, (decimal?)null),  // 5x Software-Lizenz Basis
                (articles[4], 1m, (decimal?)10m),   // 1x Schulung mit 10% Rabatt
            },
            "offer");

        // 2) Angebot → Auftrag → Rechnung (Vorgangskette)
        var ang = await CreateAsync(DocumentType.Offer, DocumentStatus.Accepted, DateTime.UtcNow.AddDays(-30),
            null,
            new[]
            {
                (articles[3], 10m, (decimal?)null),  // 10x Software-Lizenz Pro
                (articles[1], 3m, (decimal?)null),   // 3 Beratungstage
                (articles[7], 12m, (decimal?)null),  // 12 Monate Wartung
            },
            "offer");

        var auf = await CreateAsync(DocumentType.Order, DocumentStatus.Accepted, DateTime.UtcNow.AddDays(-25),
            ang.Id,
            new[]
            {
                (articles[3], 10m, (decimal?)null),
                (articles[1], 3m, (decimal?)null),
                (articles[7], 12m, (decimal?)null),
            },
            "");

        await CreateAsync(DocumentType.Invoice, DocumentStatus.Paid, DateTime.UtcNow.AddDays(-20),
            auf.Id,
            new[]
            {
                (articles[3], 10m, (decimal?)null),
                (articles[1], 3m, (decimal?)null),
                (articles[7], 12m, (decimal?)null),
            },
            "invoice");

        // 3) Schnellrechnung — Versendet, noch offen
        await CreateAsync(DocumentType.Invoice, DocumentStatus.Sent, DateTime.UtcNow.AddDays(-7),
            null,
            new[]
            {
                (articles[0], 6m, (decimal?)null),   // 6 Beratungsstunden
                (articles[6], 1m, (decimal?)null),   // 1x Express
            },
            "invoice");

        logger.LogInformation("Sample articles + documents seeded.");
    }
}
