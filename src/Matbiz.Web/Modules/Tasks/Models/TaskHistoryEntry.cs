using System.ComponentModel.DataAnnotations;

namespace Matbiz.Web.Modules.Tasks.Models;

/// <summary>
/// Audit row for a single change on a <see cref="TaskItem"/>. Mirrors the
/// CustomerHistoryEntry shape so the rendering / actor-resolution patterns
/// can be reused.
/// </summary>
public class TaskHistoryEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TaskId { get; set; }
    public TaskItem Task { get; set; } = default!;

    public DateTime At { get; set; } = DateTime.UtcNow;

    /// <summary>Identity user id of the actor that performed the action (may be the impersonation target).</summary>
    public string ActorUserId { get; set; } = string.Empty;

    /// <summary>If the actor was acting via impersonation, the real admin user id is recorded here for audit.</summary>
    public string? OnBehalfOfAdminId { get; set; }

    [Required, MaxLength(50)]
    public string Action { get; set; } = string.Empty;

    public string? Details { get; set; }
}
