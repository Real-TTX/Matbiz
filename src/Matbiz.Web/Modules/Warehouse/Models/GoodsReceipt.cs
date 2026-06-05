using System.ComponentModel.DataAnnotations;
using Matbiz.Web.Modules.Articles.Models;
using Matbiz.Web.Modules.Customers.Models;

namespace Matbiz.Web.Modules.Warehouse.Models;

public enum GoodsReceiptStatus
{
    Draft = 0,       // Bearbeitbar, noch nicht im Bestand
    Booked = 1,      // Gebucht — Bestand erhöht, immutable
    Cancelled = 2    // Storniert — Gegenbewegung gebucht
}

/// <summary>
/// Wareneingangs-Beleg: was ist von wem ins Lager geliefert worden.
/// Beim „Buchen" werden pro Position StockMovements vom Typ Receipt
/// erzeugt und der StockLevel aktualisiert.
/// </summary>
public class GoodsReceipt
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(50)]
    public string Number { get; set; } = string.Empty;

    public GoodsReceiptStatus Status { get; set; } = GoodsReceiptStatus.Draft;

    public DateTime ReceiptDate { get; set; } = DateTime.UtcNow;

    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = default!;

    /// <summary>Lieferant — Company kann gleichzeitig Kunde + Lieferant sein.</summary>
    public Guid? SupplierCompanyId { get; set; }
    public Company? SupplierCompany { get; set; }

    [MaxLength(100)]
    public string? SupplierReferenceNumber { get; set; }  // Liefer-/Bestell-Nr beim Lieferanten

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    [Required, MaxLength(450)]
    public string CreatedByUserId { get; set; } = string.Empty;

    public List<GoodsReceiptPosition> Positions { get; set; } = new();
}

public class GoodsReceiptPosition
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ReceiptId { get; set; }
    public GoodsReceipt Receipt { get; set; } = default!;

    public int Position { get; set; }

    public Guid ArticleId { get; set; }
    public Article Article { get; set; } = default!;

    [MaxLength(50)]
    public string? ArticleNumberSnapshot { get; set; }
    [Required, MaxLength(500)]
    public string DescriptionSnapshot { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    /// <summary>Einkaufspreis-Snapshot (für Marge-Berechnung später). Optional.</summary>
    public decimal? PurchasePrice { get; set; }
}
