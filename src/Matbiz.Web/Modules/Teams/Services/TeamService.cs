using Matbiz.Web.Data;
using Matbiz.Web.Modules.Teams.Models;
using Microsoft.EntityFrameworkCore;

namespace Matbiz.Web.Modules.Teams.Services;

public class TeamService(ApplicationDbContext db)
{
    public Task<List<Team>> ListAsync(CancellationToken ct = default) =>
        db.Teams.AsNoTracking()
            .Include(t => t.Members)
            .Include(t => t.Department)
            .OrderBy(t => t.Department!.Name).ThenBy(t => t.Name)
            .ToListAsync(ct);

    public Task<Team?> GetAsync(Guid id, CancellationToken ct = default) =>
        db.Teams.Include(t => t.Members).Include(t => t.Department).FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<Team> CreateAsync(Team team, CancellationToken ct = default)
    {
        db.Teams.Add(team);
        await db.SaveChangesAsync(ct);
        return team;
    }

    public async Task UpdateAsync(Team team, CancellationToken ct = default)
    {
        db.Teams.Update(team);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var t = await db.Teams.FindAsync([id], ct);
        if (t is null) return;
        db.Teams.Remove(t);
        await db.SaveChangesAsync(ct);
    }

    public async Task AddMemberAsync(Guid teamId, string userId, CancellationToken ct = default)
    {
        var exists = await db.TeamMembers.AnyAsync(m => m.TeamId == teamId && m.UserId == userId, ct);
        if (exists) return;
        db.TeamMembers.Add(new TeamMember { TeamId = teamId, UserId = userId });
        await db.SaveChangesAsync(ct);
    }

    public async Task RemoveMemberAsync(Guid teamId, string userId, CancellationToken ct = default)
    {
        var m = await db.TeamMembers.FirstOrDefaultAsync(x => x.TeamId == teamId && x.UserId == userId, ct);
        if (m is null) return;
        db.TeamMembers.Remove(m);
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Teams the given user belongs to — used to build shared task lists.</summary>
    public Task<List<Team>> ListForUserAsync(string userId, CancellationToken ct = default) =>
        db.Teams.AsNoTracking()
            .Where(t => t.Members.Any(m => m.UserId == userId))
            .OrderBy(t => t.Name)
            .ToListAsync(ct);
}
