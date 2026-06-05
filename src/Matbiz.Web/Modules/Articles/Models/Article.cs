using System.ComponentModel.DataAnnotations;

namespace Matbiz.Web.Modules.Articles.Models;

public enum ArticleType
{
    Product = 0,  // Physisches Produkt
    Service = 1   // Dienstleistung (Std/Tag/Pauschal)
}

/// <summary>
/// Stammdaten-Artikel — Position für Belege. Preis ist NETTO, Steuersatz separat
/// (statt fest 19% einzubacken — wegen 7%/0% Sonderfälle und Auslandsfälle).
/// Nummer kommt vom <see cref="Services.NumberRangeService"/>.
/// </summary>
public class Article
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Eindeutige Artikelnummer aus dem Nummernkreis „Article".</summary>
    [Required, MaxLength(50)]
    public string Number { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public ArticleType Type { get; set; } = ArticleType.Product;

    /// <summary>Mengeneinheit — frei beschreibbar (Stück, h, Tag, kg, Pauschal, …).</summary>
    [MaxLength(20)]
    public string Unit { get; set; } = "Stück";

    /// <summary>Netto-Verkaufspreis in EUR.</summary>
    public decimal NetPrice { get; set; }

    /// <summary>Optionaler Einkaufspreis für Marge-Berechnung.</summary>
    public decimal? PurchasePrice { get; set; }

    public Guid TaxRateId { get; set; }
    public TaxRate TaxRate { get; set; } = default!;

    [MaxLength(100)]
    public string? Category { get; set; }

    public bool IsActive { get; set; } = true;

    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // CustomFieldValues werden über CustomFieldService aus dem CustomFields-Modul geladen.

    // === Artikel-Bild (Haupt-Bild, inline gespeichert) ===
    public byte[]? ImageBytes { get; set; }
    [MaxLength(100)] public string? ImageContentType { get; set; }
    /// <summary>Bumped bei Bild-Upload — für Cache-Busting der Image-URL.</summary>
    public int ImageVersion { get; set; }
}
