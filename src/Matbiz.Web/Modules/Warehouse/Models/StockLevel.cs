using Matbiz.Web.Modules.Articles.Models;

namespace Matbiz.Web.Modules.Warehouse.Models;

/// <summary>
/// Bestand pro Artikel × Lager. Cache der Summe aller StockMovements —
/// wird beim Buchen eines Wareneingangs / Ausgangs aktualisiert.
/// Unique-Index auf (ArticleId, WarehouseId) verhindert Duplikate.
/// </summary>
public class StockLevel
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = default!;

    public Guid ArticleId { get; set; }
    public Article Article { get; set; } = default!;

    public decimal Quantity { get; set; }

    /// <summary>Mindestbestand — unterschritten triggert Warnung in der Übersicht.</summary>
    public decimal? ReorderLevel { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
