using System.ComponentModel.DataAnnotations;

namespace Matbiz.Web.Modules.Warehouse.Models;

/// <summary>
/// Lager-Standort. Default-Setup: ein „Hauptlager" — Multi-Standort
/// (z.B. „Werkstatt Süd", „Außenstelle Hamburg") wird durch Anlegen
/// weiterer Warehouses unterstützt.
/// </summary>
public class Warehouse
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
