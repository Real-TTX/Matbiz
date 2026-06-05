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

    /// <summary>Umsatzsteuer-Identifikationsnummer des Käufers — Pflicht bei
    /// EU-B2B-Reverse-Charge und Drittland-Geschäften (ZUGFeRD BT-48).</summary>
    [MaxLength(30)]
    public string? VatId { get; set; }

    /// <summary>Debitor-Konto für Buchhaltung (DATEV) — automatisch vergeben
    /// beim ersten Export, kann manuell überschrieben werden.</summary>
    [MaxLength(10)]
    public string? DebitorAccount { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // CustomFieldValues werden über CustomFieldService aus dem CustomFields-Modul geladen.
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

// CustomFieldType, CustomerFieldDefinition, CustomerFieldValue wurden in das
// CustomFields-Modul migriert (siehe Modules/CustomFields/Models/).

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

    /// <summary>Zeitpunkt der letzten Bearbeitung — null wenn unverändert.</summary>
    public DateTime? EditedAt { get; set; }

    /// <summary>Wer den Eintrag zuletzt bearbeitet hat.</summary>
    public string? EditedByUserId { get; set; }
}
