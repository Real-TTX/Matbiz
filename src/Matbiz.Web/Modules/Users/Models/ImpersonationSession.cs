using System.ComponentModel.DataAnnotations;

namespace Matbiz.Web.Modules.Users.Models;

/// <summary>
/// Server-side record of an admin impersonating another user. The active record
/// drives <see cref="Matbiz.Web.Impersonation.IImpersonationService"/> on every request,
/// so the effective principal cannot be tampered with from the client.
/// </summary>
public class ImpersonationSession
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public string AdminUserId { get; set; } = string.Empty;

    [Required]
    public string TargetUserId { get; set; } = string.Empty;

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    public DateTime? EndedAt { get; set; }

    [MaxLength(500)]
    public string? Reason { get; set; }

    public bool IsActive => EndedAt == null;
}
