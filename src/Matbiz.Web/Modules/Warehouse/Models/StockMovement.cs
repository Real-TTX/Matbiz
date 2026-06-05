using System.ComponentModel.DataAnnotations;
using Matbiz.Web.Modules.Articles.Models;

namespace Matbiz.Web.Modules.Warehouse.Models;

public enum StockMovementType
{
    Receipt = 0,     // Wareneingang (+)
    Issue = 1,       // Ausgang (-)
    Transfer = 2,    // Umlagerung — wird als 2 Bewegungen erfasst (Issue + Receipt)
    Adjustment = 3   // Inventur-Korrektur (+ oder -)
}

/// <summary>
/// Ein Eintrag im Bewegungs-Log. Immutable: was gebucht ist, bleibt drin
/// (Audit-Trail). Stornieren passiert durch Gegenbewegung, nicht durch Löschen.
/// </summary>
public class StockMovement
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public DateTime At { get; set; } = DateTime.UtcNow;

    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = default!;

    public Guid ArticleId { get; set; }
    public Article Article { get; set; } = default!;

    public StockMovementType Type { get; set; }

    /// <summary>Vorzeichenbehaftet — positiv = Zugang, negativ = Abgang.</summary>
    public decimal Quantity { get; set; }

    /// <summary>Optionale Referenz auf den auslösenden Beleg (Wareneingang, Rechnung, Inventur).</summary>
    [MaxLength(50)]
    public string? Reference { get; set; }

    [MaxLength(500)]
    public string? Note { get; set; }

    [Required, MaxLength(450)]
    public string CreatedByUserId { get; set; } = string.Empty;
}
