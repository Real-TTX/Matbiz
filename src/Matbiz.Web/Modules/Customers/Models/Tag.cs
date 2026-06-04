using System.ComponentModel.DataAnnotations;

namespace Matbiz.Web.Modules.Customers.Models;

public class Tag
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Hex color #RRGGBB used to render the chip.</summary>
    [MaxLength(9)]
    public string Color { get; set; } = "#6e7781";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<CustomerTag> Customers { get; set; } = new();
    public List<CompanyTag> Companies { get; set; } = new();
}

public class CustomerTag
{
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = default!;

    public Guid TagId { get; set; }
    public Tag Tag { get; set; } = default!;
}

public class CompanyTag
{
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = default!;

    public Guid TagId { get; set; }
    public Tag Tag { get; set; } = default!;
}
