using System.Globalization;
using System.Text;
using Matbiz.Web.Data;
using Matbiz.Web.Modules.Customers.Models;
using Matbiz.Web.Modules.Documents.Models;
using Matbiz.Web.Modules.SystemSettings.Services;
using Microsoft.EntityFrameworkCore;

namespace Matbiz.Web.Modules.Accounting.Services;

/// <summary>
/// Erzeugt DATEV-EXTF v7 Buchungsstapel als CSV (Semikolon-getrennt,
/// Windows-1252-Codierung — was DATEV standardmäßig erwartet).
///
/// Eine Rechnung = ein Buchungssatz pro Steuersatz/-kategorie (also typisch
/// 1 Zeile bei nur 19% Positionen, 2 Zeilen wenn mixed 19% + 7%).
///
/// Debitor-Konto wird beim ersten Export für jeden Kunden automatisch
/// vergeben (NextDebitorNumber im Branding hochgezählt). Manuell pflegbar
/// pro Customer/Company.
/// </summary>
public class DatevExporter(ApplicationDbContext db, BrandingService branding)
{
    /// <summary>Exportiert alle Rechnungen + Gutschriften im gegebenen Zeitraum.</summary>
    public async Task<byte[]> ExportAsync(DateTime fromDate, DateTime toDate, CancellationToken ct = default)
    {
        var brand = await branding.GetAsync(ct);
        var chart = brand.ChartOfAccounts ?? "SKR03";

        var docs = await db.Documents
            .Include(d => d.Customer)
            .Include(d => d.Company)
            .Include(d => d.Positions)
            .Where(d => (d.Type == DocumentType.Invoice || d.Type == DocumentType.CreditNote)
                       && d.Status != DocumentStatus.Cancelled && d.Status != DocumentStatus.Draft
                       && d.DocumentDate >= fromDate && d.DocumentDate <= toDate)
            .OrderBy(d => d.DocumentDate)
            .ThenBy(d => d.Number)
            .ToListAsync(ct);

        // Debitor-Nummern sicherstellen — fehlende werden jetzt vergeben & gespeichert.
        await EnsureDebitorAccountsAsync(docs, ct);
        await db.SaveChangesAsync(ct);

        var rows = BuildBookingRows(docs, chart);

        return EncodeCsv(rows, brand, fromDate, toDate);
    }

    private async Task EnsureDebitorAccountsAsync(List<Document> docs, CancellationToken ct)
    {
        var branding = await db.BrandingSettings.FirstOrDefaultAsync(ct);
        if (branding is null) return;

        foreach (var d in docs)
        {
            // Beim Kontakt direkt
            if (d.Customer is not null && string.IsNullOrEmpty(d.Customer.DebitorAccount))
            {
                d.Customer.DebitorAccount = branding.NextDebitorNumber.ToString(CultureInfo.InvariantCulture);
                branding.NextDebitorNumber++;
            }
            // Bei der Firma falls kein Kontakt
            else if (d.Customer is null && d.Company is not null && string.IsNullOrEmpty(d.Company.DebitorAccount))
            {
                d.Company.DebitorAccount = branding.NextDebitorNumber.ToString(CultureInfo.InvariantCulture);
                branding.NextDebitorNumber++;
            }
        }
        branding.UpdatedAt = DateTime.UtcNow;
    }

    private record BookingRow(
        decimal Amount,
        string SollHaben,
        string AccountNumber,
        string OffsetAccount,
        string BuKey,
        DateTime DocDate,
        string DocNumber,
        string Text);

    private static List<BookingRow> BuildBookingRows(List<Document> docs, string chart)
    {
        var rows = new List<BookingRow>();
        foreach (var d in docs)
        {
            var debitor = d.Customer?.DebitorAccount ?? d.Company?.DebitorAccount
                          ?? AccountMappings.DebitorCollective(chart);
            var name = d.CustomerNameSnapshot ?? "";
            var docNumber = d.Number;

            // Gruppiere Positionen nach Steuersatz × Kategorie → eine Zeile pro Gruppe
            var byTax = d.Positions
                .GroupBy(p => (Percent: p.TaxRatePercent, Cat: (p.VatCategoryCode ?? "S").ToUpperInvariant()))
                .Select(g => new
                {
                    g.Key.Percent,
                    g.Key.Cat,
                    Gross = g.Sum(p => p.GrossTotal),
                    Net   = g.Sum(p => p.NetTotal)
                });

            foreach (var grp in byTax)
            {
                var map = AccountMappings.Resolve(chart, grp.Percent, grp.Cat);
                // Rechnung: Debitor SOLL an Erlös HABEN. DATEV-Konvention:
                //   Konto = Debitor, Gegenkonto = Erlös, Soll/Haben-Kennzeichen = "S".
                // Gutschrift dreht Vorzeichen: "H".
                var soha = d.Type == DocumentType.CreditNote ? "H" : "S";

                rows.Add(new BookingRow(
                    Amount: grp.Gross,  // Brutto bei BU-0 (Konto ist Automatikkonto, USt wird intern gesplittet)
                    SollHaben: soha,
                    AccountNumber: debitor,
                    OffsetAccount: map.RevenueAccount,
                    BuKey: map.BuKey,
                    DocDate: d.DocumentDate,
                    DocNumber: docNumber,
                    Text: name.Length > 60 ? name[..60] : name));
            }
        }
        return rows;
    }

    private static byte[] EncodeCsv(List<BookingRow> rows, BrandingSettings brand, DateTime from, DateTime to)
    {
        // Windows-1252 ist DATEV-Standard. .NET braucht CodePagesEncodingProvider für Latin-1.
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var enc = Encoding.GetEncoding(1252);

        var sb = new StringBuilder();

        // --- DATEV-Header (Zeile 1): Format-Beschreibung mit 30 festen Spalten ---
        // Referenz: DATEV-Format EXTF v7
        string Csv(params string?[] cells) => string.Join(";", cells.Select(QuoteIfNeeded));

        // Berater-/Mandanten-Nummer-Placeholder: 0 / 0 — User kann später nachpflegen.
        var year = from.Year.ToString(CultureInfo.InvariantCulture);
        var wjBeginn = $"{year}0101";  // Wirtschaftsjahres-Beginn
        var fromS = from.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        var toS   = to.ToString("yyyyMMdd",   CultureInfo.InvariantCulture);
        var nowS  = DateTime.Now.ToString("yyyyMMddHHmmssfff", CultureInfo.InvariantCulture);

        sb.AppendLine(Csv(
            "EXTF",                     // 1  Kennzeichen
            "700",                      // 2  Versionsnummer
            "21",                       // 3  Daten-Kategorie (21 = Buchungsstapel)
            "Buchungsstapel",           // 4  Format-Name
            "7",                        // 5  Format-Version
            nowS,                       // 6  Erzeugt am
            "",                         // 7  Importiert (leer beim Export)
            "RE",                       // 8  Herkunft (frei)
            (brand.AppName ?? "Matbiz"),// 9  Exportiert von
            "",                         // 10 Importiert von
            "0",                        // 11 Berater-Nr (User pflegt nach)
            "0",                        // 12 Mandanten-Nr
            wjBeginn,                   // 13 WJ-Beginn
            "4",                        // 14 Sachkontenlänge
            fromS,                      // 15 Datum von
            toS,                        // 16 Datum bis
            "Belege",                   // 17 Bezeichnung
            "",                         // 18 Diktatkürzel
            "1",                        // 19 Buchungstyp (1 = Finanzbuchhaltung)
            "0",                        // 20 Rechnungslegungszweck
            "0",                        // 21 Festschreibung
            "EUR",                      // 22 WKZ
            "",                         // 23..30 frei
            "", "", "", "", "", "", "", ""
        ));

        // --- Spalten-Header (Zeile 2) — mindestens die wichtigsten Felder ---
        sb.AppendLine(Csv(
            "Umsatz (ohne Soll/Haben-Kz)",   // 1
            "Soll/Haben-Kennzeichen",        // 2
            "WKZ Umsatz",                    // 3
            "Kurs",                          // 4
            "Basis-Umsatz",                  // 5
            "WKZ Basis-Umsatz",              // 6
            "Konto",                         // 7
            "Gegenkonto (ohne BU-Schlüssel)",// 8
            "BU-Schlüssel",                  // 9
            "Belegdatum",                    // 10
            "Belegfeld 1",                   // 11
            "Belegfeld 2",                   // 12
            "Skonto",                        // 13
            "Buchungstext"                   // 14
        ));

        // --- Daten ---
        foreach (var r in rows)
        {
            sb.AppendLine(Csv(
                r.Amount.ToString("0.00", CultureInfo.GetCultureInfo("de-DE")),
                r.SollHaben,
                "EUR",
                "",
                "",
                "",
                r.AccountNumber,
                r.OffsetAccount,
                r.BuKey,
                r.DocDate.ToString("ddMM", CultureInfo.InvariantCulture),  // DATEV nutzt DDMM
                r.DocNumber,
                "",
                "",
                r.Text
            ));
        }

        return enc.GetBytes(sb.ToString());
    }

    private static string QuoteIfNeeded(string? s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        // DATEV erwartet Strings in doppelten Anführungszeichen, Zahlen ohne.
        bool isNumeric = decimal.TryParse(s, NumberStyles.Any,
            CultureInfo.GetCultureInfo("de-DE"), out _);
        if (isNumeric) return s;
        if (s.Contains('"')) s = s.Replace("\"", "\"\"");
        return $"\"{s}\"";
    }
}
