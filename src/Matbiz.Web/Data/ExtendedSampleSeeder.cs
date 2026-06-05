using Matbiz.Web.Modules.CustomMenu.Models;
using Matbiz.Web.Modules.Warehouse.Models;
using Matbiz.Web.Modules.Warehouse.Services;
using Matbiz.Web.Modules.Wiki.Models;
using Microsoft.EntityFrameworkCore;

namespace Matbiz.Web.Data;

/// <summary>
/// Demo-Daten für die neueren Module die der ältere SampleDataSeeder noch
/// nicht abdeckt: Wiki-Seiten, Custom-Menu-Einträge, Lager-Bestände +
/// Wareneingangs-Beispiele.
///
/// Aktivierung wie alle Seeder: <c>Matbiz:SeedSampleData=true</c>.
/// Idempotent — jeder Sub-Seed prüft selbst ob seine Tabelle leer ist.
/// </summary>
public static class ExtendedSampleSeeder
{
    public static async Task SeedAsync(IServiceProvider sp, IConfiguration cfg, ILogger logger, CancellationToken ct = default, bool force = false)
    {
        if (!force && !cfg.GetValue<bool>("Matbiz:SeedSampleData")) return;

        var db = sp.GetRequiredService<ApplicationDbContext>();

        await SeedWikiAsync(db, logger, ct);
        await SeedCustomMenuAsync(db, logger, ct);
        await SeedWarehouseAsync(db, sp, logger, ct);
    }

    // ─── Wiki ───────────────────────────────────────────────────────────────────

    private static async Task SeedWikiAsync(ApplicationDbContext db, ILogger logger, CancellationToken ct)
    {
        if (await db.Set<WikiPage>().AnyAsync(ct)) return;

        var pages = new List<WikiPage>
        {
            new()
            {
                Slug = "willkommen",
                Title = "Willkommen bei Matbiz",
                Visibility = WikiVisibility.Global,
                SortOrder = 0,
                ContentMarkdown = """
                    # Willkommen 👋

                    Das ist deine Wiki-Startseite. Hier kannst du **interne Doku**,
                    Prozessbeschreibungen oder Onboarding-Material ablegen.

                    ## Erste Schritte

                    1. Schau im linken Menü unter **Kontakte** wer schon angelegt ist
                    2. Erstelle deinen ersten Beleg unter **Auftragsbearbeitung → Neuer Beleg**
                    3. Im **System**-Bereich kannst du Branding (Logo + Farben) anpassen

                    > Markdown wird hier voll unterstützt — inkl. Tabellen, Code-Blöcke
                    > und Bilder.
                    """
            },
            new()
            {
                Slug = "rechnungsstellung",
                Title = "Rechnungsstellung — Ablauf",
                Visibility = WikiVisibility.Global,
                SortOrder = 10,
                ContentMarkdown = """
                    # Rechnungsstellung

                    ## Workflow

                    | Schritt | Beschreibung |
                    |---------|--------------|
                    | 1 | Angebot erstellen (Status: *Entwurf*) |
                    | 2 | Angebot versenden → Status *Versendet* |
                    | 3 | Bei Annahme: Konvertieren in **Auftrag** |
                    | 4 | Nach Lieferung: Auftrag → **Rechnung** |
                    | 5 | Bei Zahlungseingang: Status auf *Bezahlt* setzen |

                    ## ZUGFeRD / XRechnung

                    Beim PDF-Export werden automatisch die XML-Daten eingebettet
                    (Hybrid-PDF). Reine XRechnung gibt's via *XML herunterladen*-Button.

                    ## DATEV

                    Admin → `/Documents/export/datev` mit Zeitraum → CSV-Buchungsstapel.
                    """
            },
            new()
            {
                Slug = "lagerverwaltung",
                Title = "Lager — Wareneingänge buchen",
                Visibility = WikiVisibility.Global,
                SortOrder = 20,
                ContentMarkdown = """
                    # Lager / Wareneingänge

                    1. **Lager → Wareneingänge → „Neuer Wareneingang"**
                    2. Lieferant wählen (optional — kann auch leer bleiben für interne Buchungen)
                    3. Positionen hinzufügen: Artikel über den Picker auswählen, Menge eintragen
                    4. **„Buchen"** → Bestand wird sofort erhöht, Beleg wird unveränderlich

                    Stornieren funktioniert über eine Gegenbewegung (Folge-Version).
                    Mindestbestände unter `/Articles/Edit/{id}` pflegen.
                    """
            }
        };

        db.AddRange(pages);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Seeded {Count} Wiki sample pages.", pages.Count);
    }

    // ─── Custom Menu ────────────────────────────────────────────────────────────

    private static async Task SeedCustomMenuAsync(ApplicationDbContext db, ILogger logger, CancellationToken ct)
    {
        if (await db.Set<CustomMenuItem>().AnyAsync(ct)) return;

        var items = new List<CustomMenuItem>
        {
            new()
            {
                Label = "GitHub Repo",
                Url = "https://github.com/Real-TTX/Matbiz",
                IconClass = "bi-github",
                OpenMode = CustomMenuOpenMode.NewTab,
                Context = CustomMenuContext.Sidebar,
                Visibility = CustomMenuVisibility.Global,
                SortOrder = 100
            },
            new()
            {
                Label = "Telefonsuche (Beispiel)",
                Url = "https://www.dasoertliche.de/?kw={Phone}",
                IconClass = "bi-telephone-outbound",
                OpenMode = CustomMenuOpenMode.NewTab,
                Context = CustomMenuContext.ContactDetail,
                Visibility = CustomMenuVisibility.Global,
                SortOrder = 10
            },
            new()
            {
                Label = "Firma im Handelsregister",
                Url = "https://www.northdata.de/{CompanyName}",
                IconClass = "bi-building-check",
                OpenMode = CustomMenuOpenMode.NewTab,
                Context = CustomMenuContext.CompanyDetail,
                Visibility = CustomMenuVisibility.Global,
                SortOrder = 10
            }
        };

        db.AddRange(items);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Seeded {Count} Custom-Menu sample items.", items.Count);
    }

    // ─── Warehouse ──────────────────────────────────────────────────────────────

    private static async Task SeedWarehouseAsync(ApplicationDbContext db, IServiceProvider sp, ILogger logger, CancellationToken ct)
    {
        // Wenn schon Bestände oder Wareneingänge existieren → nichts tun
        if (await db.Set<StockLevel>().AnyAsync(ct) || await db.Set<GoodsReceipt>().AnyAsync(ct))
            return;

        var warehouse = await db.Set<Matbiz.Web.Modules.Warehouse.Models.Warehouse>()
            .OrderByDescending(w => w.IsDefault).FirstOrDefaultAsync(ct);
        if (warehouse is null) return;  // EnsureDefault sollte vorher laufen — defensive

        // Artikel zum Bebuchen — falls keine vorhanden, abbrechen
        var articles = await db.Articles.Take(5).ToListAsync(ct);
        if (articles.Count == 0) return;

        // Mindestbestände für die Artikel setzen + Initial-Bestand via Wareneingang
        var stock = sp.GetRequiredService<StockService>();
        var receiptService = sp.GetRequiredService<GoodsReceiptService>();
        var numbers = sp.GetRequiredService<Matbiz.Web.Modules.Articles.Services.NumberRangeService>();

        // Beispiel-Lieferant (erste Company als Lieferant)
        var supplier = await db.Companies.OrderBy(c => c.CreatedAt).FirstOrDefaultAsync(ct);

        // === 1. Gebuchter Wareneingang: Eröffnungsbestand ===
        var openingNumber = await numbers.NextAsync("GoodsReceipt", ct);
        var opening = new GoodsReceipt
        {
            Number = openingNumber,
            Status = GoodsReceiptStatus.Booked,
            ReceiptDate = DateTime.UtcNow.AddDays(-30),
            WarehouseId = warehouse.Id,
            SupplierCompanyId = supplier?.Id,
            SupplierReferenceNumber = "LS-2026-001",
            Note = "Eröffnungs-Wareneingang (Demo-Daten).",
            CreatedByUserId = string.Empty,
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            UpdatedAt = DateTime.UtcNow.AddDays(-30)
        };
        int pos = 1;
        var rng = new Random(42);
        foreach (var a in articles)
        {
            opening.Positions.Add(new GoodsReceiptPosition
            {
                Position = pos++,
                ArticleId = a.Id,
                ArticleNumberSnapshot = a.Number,
                DescriptionSnapshot = a.Name,
                Quantity = rng.Next(20, 100),
                PurchasePrice = a.PurchasePrice
            });
        }
        db.GoodsReceipts.Add(opening);
        await db.SaveChangesAsync(ct);

        // StockLevels manuell setzen + Movements protokollieren (da wir Status=Booked
        // direkt setzen statt durch BookAsync zu gehen — vermeidet Probleme mit
        // fehlendem CurrentUser im Seed-Context)
        foreach (var p in opening.Positions)
        {
            db.StockMovements.Add(new StockMovement
            {
                At = opening.ReceiptDate,
                WarehouseId = warehouse.Id,
                ArticleId = p.ArticleId,
                Type = StockMovementType.Receipt,
                Quantity = p.Quantity,
                Reference = opening.Number,
                Note = "Eröffnungsbestand (Demo)",
                CreatedByUserId = string.Empty
            });
            db.StockLevels.Add(new StockLevel
            {
                ArticleId = p.ArticleId,
                WarehouseId = warehouse.Id,
                Quantity = p.Quantity,
                ReorderLevel = Math.Max(5m, Math.Round(p.Quantity * 0.2m))
            });
        }
        await db.SaveChangesAsync(ct);

        // === 2. Aktueller Entwurf eines Wareneingangs (zum Anschauen im UI) ===
        var draftNumber = await numbers.NextAsync("GoodsReceipt", ct);
        var draft = new GoodsReceipt
        {
            Number = draftNumber,
            Status = GoodsReceiptStatus.Draft,
            ReceiptDate = DateTime.UtcNow.Date,
            WarehouseId = warehouse.Id,
            SupplierCompanyId = supplier?.Id,
            SupplierReferenceNumber = "LS-2026-014",
            Note = "Entwurf — kann im UI verändert + gebucht werden.",
            CreatedByUserId = string.Empty
        };
        int p2 = 1;
        foreach (var a in articles.Take(3))
        {
            draft.Positions.Add(new GoodsReceiptPosition
            {
                Position = p2++,
                ArticleId = a.Id,
                ArticleNumberSnapshot = a.Number,
                DescriptionSnapshot = a.Name,
                Quantity = rng.Next(5, 25),
                PurchasePrice = a.PurchasePrice
            });
        }
        db.GoodsReceipts.Add(draft);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Seeded Warehouse: 1 booked receipt, 1 draft, {N} stock levels.",
            opening.Positions.Count);
    }
}
