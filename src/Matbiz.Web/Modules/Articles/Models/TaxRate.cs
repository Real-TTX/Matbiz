using System.ComponentModel.DataAnnotations;

namespace Matbiz.Web.Modules.Articles.Models;

/// <summary>
/// Steuersatz-Stammdaten. Default-Set DE: 19% / 7% / 0% — admin-konfigurierbar.
/// Wird auf Artikel verwiesen und beim Beleg in jede Position eingefroren
/// (snapshot Percent), damit historische Belege unverändert bleiben.
/// </summary>
public class TaxRate
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Prozentsatz, z.B. 19.0, 7.0, 0.0.</summary>
    public decimal Percent { get; set; }

    /// <summary>Default für neue Artikel.</summary>
    public bool IsDefault { get; set; }

    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
