using System.ComponentModel.DataAnnotations;

namespace Matbiz.Web.Modules.Customers.Models;

public class Company
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(200)]
    public string? Email { get; set; }

    [MaxLength(50)]
    public string? Phone { get; set; }

    [MaxLength(200)]
    public string? Street { get; set; }

    [MaxLength(20)]
    public string? PostalCode { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(100)]
    public string? Country { get; set; }

    /// <summary>Umsatzsteuer-ID des Käufer-Firma (ZUGFeRD BT-48).</summary>
    [MaxLength(30)]
    public string? VatId { get; set; }

    /// <summary>Leitweg-ID für B2G-XRechnung (Bundes/Land/Kommune).</summary>
    [MaxLength(50)]
    public string? BuyerReference { get; set; }

    /// <summary>Debitor-Konto für DATEV-Export (automatisch vergeben).</summary>
    [MaxLength(10)]
    public string? DebitorAccount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<Customer> Contacts { get; set; } = new();
    public List<CompanyTag> Tags { get; set; } = new();
    public List<CompanyHistoryEntry> History { get; set; } = new();

    public string? LocationLine =>
        (string.IsNullOrEmpty(PostalCode), string.IsNullOrEmpty(City)) switch
        {
            (true, true) => null,
            (true, false) => City,
            (false, true) => PostalCode,
            _ => $"{PostalCode} {City}"
        };
}
