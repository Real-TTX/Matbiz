using Matbiz.Web.Data;
using Matbiz.Web.Modules.Users.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Matbiz.Web.Impersonation;

public class ImpersonationService(
    ApplicationDbContext db,
    UserManager<ApplicationUser> users,
    ILogger<ImpersonationService> log) : IImpersonationService
{
    public async Task<ImpersonationSession> StartAsync(string adminUserId, string targetUserId, string? reason, CancellationToken ct = default)
    {
        if (string.Equals(adminUserId, targetUserId, StringComparison.Ordinal))
            throw new InvalidOperationException("Selbst-Impersonation ist nicht erlaubt.");

        var admin = await users.FindByIdAsync(adminUserId)
            ?? throw new InvalidOperationException("Admin user not found.");
        if (!await users.IsInRoleAsync(admin, Roles.Admin))
            throw new UnauthorizedAccessException("Only admins may start impersonation.");

        var target = await users.FindByIdAsync(targetUserId)
            ?? throw new InvalidOperationException("Target user not found.");
        if (!target.IsActive)
            throw new InvalidOperationException("Target user is inactive.");

        // End any previous active session for this admin first.
        await EndAsync(adminUserId, ct);

        var session = new ImpersonationSession
        {
            AdminUserId = adminUserId,
            TargetUserId = targetUserId,
            Reason = reason,
            StartedAt = DateTime.UtcNow
        };
        db.ImpersonationSessions.Add(session);
        await db.SaveChangesAsync(ct);

        log.LogWarning("Impersonation started: admin {Admin} -> target {Target}", adminUserId, targetUserId);
        return session;
    }

    public async Task EndAsync(string adminUserId, CancellationToken ct = default)
    {
        var active = await db.ImpersonationSessions
            .Where(x => x.AdminUserId == adminUserId && x.EndedAt == null)
            .ToListAsync(ct);
        if (active.Count == 0) return;

        var now = DateTime.UtcNow;
        foreach (var s in active) s.EndedAt = now;
        await db.SaveChangesAsync(ct);
        log.LogWarning("Impersonation ended: admin {Admin} ({Count} session(s))", adminUserId, active.Count);
    }

    public Task<ImpersonationSession?> GetActiveForAdminAsync(string adminUserId, CancellationToken ct = default) =>
        db.ImpersonationSessions
            .AsNoTracking()
            .Where(x => x.AdminUserId == adminUserId && x.EndedAt == null)
            .OrderByDescending(x => x.StartedAt)
            .FirstOrDefaultAsync(ct);
}
