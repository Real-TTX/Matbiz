namespace Matbiz.Web.Modules.Accounting;

/// <summary>
/// Default-Erlöskonten + DATEV-Buchungsschlüssel pro Steuersatz und
/// Steuer-Kategorie. Werte aus offiziellen SKR-Kontenrahmen — Stand 2024/25.
/// Anpassungen pro Mandant könnten später als Override-Tabelle dazukommen.
/// </summary>
public static class AccountMappings
{
    public record AccountMapping(
        string RevenueAccount,    // Erlöskonto (Gegenkonto)
        string BuKey);            // BU-Schlüssel (Steuerschlüssel)

    /// <summary>
    /// Liefert das passende Mapping für (Kontorahmen, Prozent, VAT-Kategorie).
    /// Kategorie aus <see cref="Documents.Models.DocumentPosition.VatCategoryCode"/>
    /// (S/Z/E/AE/K/G/O).
    /// </summary>
    public static AccountMapping Resolve(string chart, decimal percent, string vatCategory)
    {
        var key = (chart?.ToUpperInvariant() ?? "SKR03", percent, vatCategory?.ToUpperInvariant() ?? "S");

        // SKR03 — am häufigsten in DE
        if (key.Item1 == "SKR03")
        {
            return key switch
            {
                (_, 19m, "S") => new("8400", "0"), // Erlöse 19% USt — BU-Schlüssel "0" weil Konto bereits steuerschlüsselbehaftet (Automatik-Konto)
                (_, 7m,  "S") => new("8300", "0"), // Erlöse 7% USt
                (_, 0m,  "Z") => new("8200", "0"), // Erlöse steuerfrei ohne Vorsteuerabzug
                (_, 0m,  "K") => new("8125", "0"), // Innergemeinschaftliche Lieferung (EU 0%)
                (_, 0m,  "G") => new("8120", "0"), // Steuerfreie Umsätze (Ausfuhr Drittland)
                (_, 0m,  "AE") => new("8336", "94"), // Reverse Charge — BU 94 weiterreichend
                (_, 0m,  "E") => new("8200", "0"),  // Steuerbefreit nach § 4 UStG
                (_, 0m,  "O") => new("8195", "0"),  // Kleinunternehmer §19
                _ => new("8400", "0")               // Fallback
            };
        }

        // SKR04 — moderner, an HGB-Gliederung orientiert
        return key switch
        {
            (_, 19m, "S") => new("4400", "0"),
            (_, 7m,  "S") => new("4300", "0"),
            (_, 0m,  "Z") => new("4200", "0"),
            (_, 0m,  "K") => new("4125", "0"),
            (_, 0m,  "G") => new("4120", "0"),
            (_, 0m,  "AE") => new("4336", "94"),
            (_, 0m,  "E") => new("4200", "0"),
            (_, 0m,  "O") => new("4195", "0"),
            _ => new("4400", "0")
        };
    }

    /// <summary>Debitoren-Sammelkonto (Gegenkonto-Default wenn kein Einzeldebitor).</summary>
    public static string DebitorCollective(string chart) =>
        (chart?.ToUpperInvariant() ?? "SKR03") == "SKR03" ? "1400" : "1200";
}
