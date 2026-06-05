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

    public async Task<List<ActiveImpersonator>> ListActiveOnTargetAsync(string targetUserId, CancellationToken ct = default)
    {
        var sessions = await db.ImpersonationSessions.AsNoTracking()
            .Where(x => x.TargetUserId == targetUserId && x.EndedAt == null)
            .OrderBy(x => x.StartedAt)
            .ToListAsync(ct);
        if (sessions.Count == 0) return new();

        var adminIds = sessions.Select(s => s.AdminUserId).Distinct().ToList();
        var admins = await db.Users.AsNoTracking()
            .Where(u => adminIds.Contains(u.Id))
            .Select(u => new { u.Id, u.DisplayName, u.Email })
            .ToListAsync(ct);
        var byId = admins.ToDictionary(a => a.Id, a => a);

        return sessions.Select(s =>
        {
            byId.TryGetValue(s.AdminUserId, out var a);
            var name = a?.DisplayName ?? a?.Email ?? s.AdminUserId;
            return new ActiveImpersonator(s.AdminUserId, name, s.StartedAt);
        }).ToList();
    }
}
