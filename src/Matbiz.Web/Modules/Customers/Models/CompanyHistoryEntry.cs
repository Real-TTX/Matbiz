using System.ComponentModel.DataAnnotations;

namespace Matbiz.Web.Modules.Customers.Models;

/// <summary>
/// History row on a Company. Mirrors <see cref="CustomerHistoryEntry"/> so
/// rendering / actor-resolution patterns can be reused. The company timeline
/// can be augmented by the linked contacts' history (UI checkbox); that's a
/// view-time merge, not a stored copy.
/// </summary>
public class CompanyHistoryEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = default!;

    public DateTime At { get; set; } = DateTime.UtcNow;

    public string ActorUserId { get; set; } = string.Empty;
    public string? OnBehalfOfAdminId { get; set; }

    [Required, MaxLength(50)]
    public string Action { get; set; } = string.Empty;

    public string? Details { get; set; }

    public DateTime? EditedAt { get; set; }
    public string? EditedByUserId { get; set; }
}
