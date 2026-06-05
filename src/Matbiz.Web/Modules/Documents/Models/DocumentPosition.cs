using System.ComponentModel.DataAnnotations;
using Matbiz.Web.Modules.Articles.Models;

namespace Matbiz.Web.Modules.Documents.Models;

/// <summary>
/// Eine Position auf einem <see cref="Document"/>. Hält einen Snapshot der
/// Artikel-Daten (Nummer, Beschreibung, Preis, MwSt) — der Beleg muss
/// unverändert bleiben wenn der Artikel später anders gepflegt wird.
/// </summary>
public class DocumentPosition
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid DocumentId { get; set; }
    public Document Document { get; set; } = default!;

    /// <summary>Sortierung 1..n.</summary>
    public int Position { get; set; }

    // Optional-FK auf Artikel (nur Referenz, kein Required — freie Positionen erlaubt)
    public Guid? ArticleId { get; set; }
    public Article? Article { get; set; }

    [MaxLength(50)]
    public string? ArticleNumber { get; set; }

    [Required, MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Unit { get; set; } = "Stück";

    public decimal Quantity { get; set; } = 1m;

    public decimal NetPrice { get; set; }

    /// <summary>Rabatt in Prozent — 0 bedeutet keiner.</summary>
    public decimal DiscountPercent { get; set; }

    /// <summary>Steuersatz als Snapshot (Prozentwert), z.B. 19.0.</summary>
    public decimal TaxRatePercent { get; set; }

    /// <summary>
    /// USt-Kategorie-Code (UNTDID 5305) — wird in ZUGFeRD/XRechnung gebraucht:
    /// <list type="bullet">
    /// <item>S = Standard rate (Regelsteuersatz, 19% / 7%)</item>
    /// <item>Z = Zero rated (steuerbar mit 0%)</item>
    /// <item>E = Steuerbefreit (§ 4 UStG)</item>
    /// <item>AE = Reverse Charge (Umkehrung der Steuerschuldnerschaft)</item>
    /// <item>K = Innergemeinschaftliche Lieferung (EU 0%)</item>
    /// <item>G = Ausfuhrlieferung (Drittland 0%)</item>
    /// <item>O = Außerhalb des Steuerbereichs (Kleinunternehmer §19)</item>
    /// </list>
    /// </summary>
    [MaxLength(4)]
    public string VatCategoryCode { get; set; } = "S";

    // Berechnete Summen (persistiert für schnelle Anzeige)
    public decimal NetTotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal GrossTotal { get; set; }

    /// <summary>Berechnet alle drei Summen-Felder. NetTotal = Menge × Preis × (1−Rabatt%).</summary>
    public void Recalculate()
    {
        var net = decimal.Round(Quantity * NetPrice * (1m - DiscountPercent / 100m), 2, MidpointRounding.AwayFromZero);
        var tax = decimal.Round(net * TaxRatePercent / 100m, 2, MidpointRounding.AwayFromZero);
        NetTotal = net;
        TaxTotal = tax;
        GrossTotal = net + tax;
    }
}
