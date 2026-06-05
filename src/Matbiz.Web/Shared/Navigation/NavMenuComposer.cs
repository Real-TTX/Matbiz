using Matbiz.Web.Data;
using Matbiz.Web.Modules.Modules.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Matbiz.Web.Shared.Navigation;

/// <summary>
/// Holt alle <see cref="INavMenuProvider"/> aus DI, filtert nach Modul-Status,
/// wendet Admin-Layout-Overrides an, sortiert. Cached die Layout-Tabelle 30s.
/// </summary>
public class NavMenuComposer(
    IEnumerable<INavMenuProvider> providers,
    ModuleRegistry modules,
    ApplicationDbContext db,
    IMemoryCache cache)
{
    private const string CacheKey = "navmenu-layout";

    /// <summary>Liefert: (sichtbare Einträge sortiert) + (komplette Layout-Übersicht für Admin).</summary>
    public async Task<NavMenuComposition> ComposeAsync(NavMenuContext ctx, CancellationToken ct = default)
    {
        var raw = new List<NavMenuEntry>();
        foreach (var p in providers)
        {
            if (!string.IsNullOrEmpty(p.ModuleKey) && !modules.IsEnabled(p.ModuleKey)) continue;
            raw.AddRange(await p.GetEntriesAsync(ctx, ct));
        }

        var layout = await GetLayoutAsync(ct);
        var withOverrides = raw.Select(e =>
        {
            // Kein Override vorhanden → Default-Visibility des Providers entscheidet.
            if (!layout.TryGetValue(e.Key, out var ov)) return (Entry: e, Hidden: e.HiddenByDefault);
            var sec = ov.SectionOverride switch { null => e.Section, "" => null, _ => ov.SectionOverride };
            return (
                Entry: e with
                {
                    Label = ov.LabelOverride ?? e.Label,
                    Section = sec,
                    SortOrder = ov.SortOrderOverride ?? e.SortOrder
                },
                Hidden: ov.IsHidden
            );
        }).ToList();

        var visible = withOverrides.Where(x => !x.Hidden)
            .Select(x => x.Entry)
            .OrderBy(e => e.SortOrder)
            .ToList();

        return new NavMenuComposition(visible, withOverrides.Select(x => (x.Entry, x.Hidden)).ToList());
    }

    public void InvalidateCache() => cache.Remove(CacheKey);

    private async Task<Dictionary<string, Modules.Modules.Models.NavMenuLayout>> GetLayoutAsync(CancellationToken ct)
    {
        if (cache.TryGetValue(CacheKey, out Dictionary<string, Modules.Modules.Models.NavMenuLayout>? cached) && cached != null)
            return cached;

        var rows = await db.NavMenuLayouts.AsNoTracking().ToListAsync(ct);
        var map = rows.ToDictionary(r => r.EntryKey);
        cache.Set(CacheKey, map, TimeSpan.FromSeconds(30));
        return map;
    }
}

/// <summary>Resultat: <see cref="Visible"/> ist gefilterte+sortierte Liste für die Sidebar.
/// <see cref="All"/> ist die komplette Übersicht (inkl. versteckter Einträge) für das Admin-Layout.</summary>
public record NavMenuComposition(
    IReadOnlyList<NavMenuEntry> Visible,
    IReadOnlyList<(NavMenuEntry Entry, bool Hidden)> All);
