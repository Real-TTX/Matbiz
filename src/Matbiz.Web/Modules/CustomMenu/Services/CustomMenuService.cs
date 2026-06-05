using System.Text.RegularExpressions;
using Matbiz.Web.Data;
using Matbiz.Web.Modules.CustomMenu.Models;
using Microsoft.EntityFrameworkCore;

namespace Matbiz.Web.Modules.CustomMenu.Services;

public class CustomMenuService(ApplicationDbContext db)
{
    public Task<List<CustomMenuItem>> ListAllAsync(CancellationToken ct = default) =>
        db.CustomMenuItems.AsNoTracking()
            .Include(i => i.Departments).ThenInclude(d => d.Department)
            .Include(i => i.Teams).ThenInclude(t => t.Team)
            .OrderBy(i => i.Context).ThenBy(i => i.SortOrder).ThenBy(i => i.Label)
            .ToListAsync(ct);

    /// <summary>Items im gewählten Kontext, gefiltert auf Sichtbarkeit für User.</summary>
    public async Task<List<CustomMenuItem>> ListVisibleAsync(
        string? userId, CustomMenuContext context, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(userId)) return new();

        var myTeamIds = await db.TeamMembers.AsNoTracking()
            .Where(m => m.UserId == userId).Select(m => m.TeamId).ToListAsync(ct);
        var myDeptIds = await db.Teams.AsNoTracking()
            .Where(t => myTeamIds.Contains(t.Id) && t.DepartmentId != null)
            .Select(t => t.DepartmentId!.Value).Distinct().ToListAsync(ct);

        return await db.CustomMenuItems.AsNoTracking()
            .Where(i => i.Context == context)
            .Where(i =>
                i.Visibility == CustomMenuVisibility.Global
                || (i.Visibility == CustomMenuVisibility.Teams && i.Teams.Any(t => myTeamIds.Contains(t.TeamId)))
                || (i.Visibility == CustomMenuVisibility.Departments && i.Departments.Any(d => myDeptIds.Contains(d.DepartmentId))))
            .OrderBy(i => i.SortOrder).ThenBy(i => i.Label)
            .ToListAsync(ct);
    }

    public Task<CustomMenuItem?> GetAsync(Guid id, CancellationToken ct = default) =>
        db.CustomMenuItems
            .Include(i => i.Departments)
            .Include(i => i.Teams)
            .FirstOrDefaultAsync(i => i.Id == id, ct);

    public async Task<CustomMenuItem> CreateAsync(CustomMenuItem item, CancellationToken ct = default)
    {
        item.CreatedAt = item.UpdatedAt = DateTime.UtcNow;
        var max = await db.CustomMenuItems
            .Where(x => x.Context == item.Context)
            .MaxAsync(x => (int?)x.SortOrder, ct) ?? 0;
        item.SortOrder = max + 1;
        db.CustomMenuItems.Add(item);
        await db.SaveChangesAsync(ct);
        return item;
    }

    public async Task UpdateAsync(CustomMenuItem item, CancellationToken ct = default)
    {
        item.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var i = await db.CustomMenuItems.FindAsync([id], ct);
        if (i is null) return;
        db.CustomMenuItems.Remove(i);
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Drag-Drop-Sortierung — erwartet GUIDs in neuer Reihenfolge.</summary>
    public async Task ReorderAsync(IEnumerable<Guid> orderedIds, CancellationToken ct = default)
    {
        var ids = orderedIds.ToList();
        var items = await db.CustomMenuItems.Where(i => ids.Contains(i.Id)).ToListAsync(ct);
        for (var i = 0; i < ids.Count; i++)
        {
            var item = items.FirstOrDefault(x => x.Id == ids[i]);
            if (item != null) item.SortOrder = i + 1;
        }
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Ersetzt Platzhalter wie {Phone} oder {FirstName} per Map (URL-encoded).</summary>
    public static string SubstituteUrl(string template, IDictionary<string, string?> values)
    {
        if (string.IsNullOrEmpty(template)) return template;
        return Regex.Replace(template, @"\{(\w+)\}", m =>
        {
            var key = m.Groups[1].Value;
            if (values.TryGetValue(key, out var v) && v != null)
                return Uri.EscapeDataString(v);
            return string.Empty;
        });
    }
}
