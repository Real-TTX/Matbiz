using System.ComponentModel.DataAnnotations;

namespace Matbiz.Web.Modules.Customers.Models;

public class Customer
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional link to a structured <see cref="Company"/> record. When set,
    /// the company name comes from there; <see cref="CompanyName"/> is the
    /// freetext fallback for contacts whose company isn't a managed entity.
    /// </summary>
    public Guid? CompanyId { get; set; }
    public Company? Company { get; set; }

    [MaxLength(200)]
    public string? CompanyName { get; set; }

    [MaxLength(200), EmailAddress]
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

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<CustomerFieldValue> CustomFieldValues { get; set; } = new();
    public List<CustomerHistoryEntry> History { get; set; } = new();
    public List<CustomerTag> Tags { get; set; } = new();

    /// <summary>Display name for the company affiliation: structured Company.Name if linked, freetext CompanyName otherwise.</summary>
    public string? EffectiveCompanyName => Company?.Name ?? CompanyName;

    /// <summary>Combined "PLZ Ort" for compact display.</summary>
    public string? LocationLine =>
        (string.IsNullOrEmpty(PostalCode), string.IsNullOrEmpty(City)) switch
        {
            (true, true) => null,
            (true, false) => City,
            (false, true) => PostalCode,
            _ => $"{PostalCode} {City}"
        };
}

public enum CustomFieldType
{
    Text = 0,
    Number = 1,
    Date = 2,
    Boolean = 3,
    LongText = 4,
    File = 5
}

public class CustomerFieldDefinition
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(100)]
    public string Key { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Label { get; set; } = string.Empty;

    public CustomFieldType Type { get; set; } = CustomFieldType.Text;

    public bool Required { get; set; }

    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class CustomerFieldValue
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = default!;
    public Guid FieldDefinitionId { get; set; }
    public CustomerFieldDefinition FieldDefinition { get; set; } = default!;
    public string? Value { get; set; }
}

public class CustomerHistoryEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = default!;

    public DateTime At { get; set; } = DateTime.UtcNow;

    /// <summary>Identity user id of the actor that performed the action (may be the impersonation target).</summary>
    public string ActorUserId { get; set; } = string.Empty;

    /// <summary>If the actor was acting via impersonation, the real admin user id is recorded here for audit.</summary>
    public string? OnBehalfOfAdminId { get; set; }

    [Required, MaxLength(50)]
    public string Action { get; set; } = string.Empty;

    public string? Details { get; set; }
}
