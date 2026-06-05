using Matbiz.Web.Data;
using Matbiz.Web.Modules.Wiki.Models;
using Microsoft.EntityFrameworkCore;

namespace Matbiz.Web.Modules.Wiki.Services;

public class WikiService(ApplicationDbContext db)
{
    /// <summary>Pages the user is allowed to see. Admins get all.</summary>
    public async Task<List<WikiPage>> ListAccessibleAsync(string? userId, bool isAdmin, CancellationToken ct = default)
    {
        if (isAdmin)
        {
            return await db.WikiPages.AsNoTracking()
                .Include(p => p.Departments).ThenInclude(d => d.Department)
                .Include(p => p.Teams).ThenInclude(t => t.Team)
                .OrderBy(p => p.SortOrder).ThenBy(p => p.Title)
                .ToListAsync(ct);
        }
        if (string.IsNullOrEmpty(userId)) return new();

        var myTeamIds = await db.TeamMembers.AsNoTracking()
            .Where(m => m.UserId == userId).Select(m => m.TeamId).ToListAsync(ct);

        var myDeptIds = await db.Teams.AsNoTracking()
            .Where(t => myTeamIds.Contains(t.Id) && t.DepartmentId != null)
            .Select(t => t.DepartmentId!.Value).Distinct().ToListAsync(ct);

        return await db.WikiPages.AsNoTracking()
            .Where(p =>
                p.Visibility == WikiVisibility.Global
                || (p.Visibility == WikiVisibility.Teams && p.Teams.Any(t => myTeamIds.Contains(t.TeamId)))
                || (p.Visibility == WikiVisibility.Departments && p.Departments.Any(d => myDeptIds.Contains(d.DepartmentId)))
                || p.CreatedByUserId == userId
                || p.Editors.Any(e => e.UserId == userId))
            .Include(p => p.Departments).ThenInclude(d => d.Department)
            .Include(p => p.Teams).ThenInclude(t => t.Team)
            .OrderBy(p => p.SortOrder).ThenBy(p => p.Title)
            .ToListAsync(ct);
    }

    public Task<WikiPage?> GetBySlugAsync(string slug, CancellationToken ct = default) =>
        db.WikiPages
            .Include(p => p.Departments).ThenInclude(d => d.Department)
            .Include(p => p.Teams).ThenInclude(t => t.Team)
            .Include(p => p.Editors)
            .FirstOrDefaultAsync(p => p.Slug == slug, ct);

    public Task<WikiPage?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.WikiPages
            .Include(p => p.Departments).ThenInclude(d => d.Department)
            .Include(p => p.Teams).ThenInclude(t => t.Team)
            .Include(p => p.Editors)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<bool> CanReadAsync(WikiPage page, string? userId, bool isAdmin, CancellationToken ct = default)
    {
        if (isAdmin) return true;
        if (string.IsNullOrEmpty(userId)) return false;
        if (page.CreatedByUserId == userId) return true;
        if (page.Editors.Any(e => e.UserId == userId)) return true;
        if (page.Visibility == WikiVisibility.Global) return true;

        var myTeamIds = await db.TeamMembers.AsNoTracking()
            .Where(m => m.UserId == userId).Select(m => m.TeamId).ToListAsync(ct);
        if (page.Visibility == WikiVisibility.Teams)
            return page.Teams.Any(t => myTeamIds.Contains(t.TeamId));

        // Departments: user is in a team whose department is linked.
        var myDeptIds = await db.Teams.AsNoTracking()
            .Where(t => myTeamIds.Contains(t.Id) && t.DepartmentId != null)
            .Select(t => t.DepartmentId!.Value).Distinct().ToListAsync(ct);
        return page.Departments.Any(d => myDeptIds.Contains(d.DepartmentId));
    }

    public bool CanWrite(WikiPage page, string? userId, bool isAdmin)
    {
        if (isAdmin) return true;
        if (string.IsNullOrEmpty(userId)) return false;
        if (page.CreatedByUserId == userId) return true;
        return page.Editors.Any(e => e.UserId == userId);
    }

    public async Task<WikiPage> CreateAsync(WikiPage page, CancellationToken ct = default)
    {
        page.Slug = SlugHelper.Sanitize(string.IsNullOrWhiteSpace(page.Slug) ? page.Title : page.Slug);
        page.Slug = await EnsureUniqueSlugAsync(page.Slug, page.Id, ct);
        page.CreatedAt = page.UpdatedAt = DateTime.UtcNow;
        db.WikiPages.Add(page);
        await db.SaveChangesAsync(ct);
        return page;
    }

    public async Task UpdateAsync(WikiPage page, CancellationToken ct = default)
    {
        page.UpdatedAt = DateTime.UtcNow;
        page.Slug = SlugHelper.Sanitize(page.Slug);
        page.Slug = await EnsureUniqueSlugAsync(page.Slug, page.Id, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var p = await db.WikiPages.FindAsync([id], ct);
        if (p is null) return;
        db.WikiPages.Remove(p);
        await db.SaveChangesAsync(ct);
    }

    private async Task<string> EnsureUniqueSlugAsync(string baseSlug, Guid currentId, CancellationToken ct)
    {
        var slug = baseSlug;
        int n = 2;
        while (await db.WikiPages.AnyAsync(p => p.Slug == slug && p.Id != currentId, ct))
            slug = $"{baseSlug}-{n++}";
        return slug;
    }
}

public static class SlugHelper
{
    private static readonly string[] Reserved = { "create", "edit", "new", "delete" };

    /// <summary>Convert a title to a URL-safe slug: lowercase ASCII, hyphens.</summary>
    public static string Sanitize(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "page";
        var s = input.Trim().ToLowerInvariant()
            .Replace("ä", "ae").Replace("ö", "oe").Replace("ü", "ue").Replace("ß", "ss");
        var sb = new System.Text.StringBuilder();
        foreach (var ch in s)
            sb.Append(char.IsLetterOrDigit(ch) ? ch : '-');
        var result = System.Text.RegularExpressions.Regex.Replace(sb.ToString(), "-+", "-").Trim('-');
        if (string.IsNullOrEmpty(result)) result = "page";
        if (Reserved.Contains(result)) result += "-page";
        return result;
    }
}
