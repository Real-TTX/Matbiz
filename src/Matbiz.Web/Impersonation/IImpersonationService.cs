using Matbiz.Web.Modules.Users.Models;

namespace Matbiz.Web.Impersonation;

/// <summary>
/// Server-side gatekeeper for the admin "Remote Zugriff" feature. The active
/// impersonation record in the database is the single source of truth — clients
/// cannot opt themselves into another user's session.
/// </summary>
public interface IImpersonationService
{
    Task<ImpersonationSession> StartAsync(string adminUserId, string targetUserId, string? reason, CancellationToken ct = default);

    Task EndAsync(string adminUserId, CancellationToken ct = default);

    Task<ImpersonationSession?> GetActiveForAdminAsync(string adminUserId, CancellationToken ct = default);

    /// <summary>Liste aller Admins die aktuell auf den User „remoten" — mit Anzeige-Namen.</summary>
    Task<List<ActiveImpersonator>> ListActiveOnTargetAsync(string targetUserId, CancellationToken ct = default);
}

public record ActiveImpersonator(string AdminUserId, string AdminDisplayName, DateTime StartedAt);
