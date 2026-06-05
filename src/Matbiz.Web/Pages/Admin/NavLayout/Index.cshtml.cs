using Matbiz.Web.Data;
using Matbiz.Web.Modules.Modules.Models;
using Matbiz.Web.Shared;
using Matbiz.Web.Shared.Navigation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Matbiz.Web.Pages.Admin.NavLayout;

[Authorize(Roles = "Admin")]
public class IndexModel(
    NavMenuComposer composer,
    ApplicationDbContext db,
    ICurrentUserAccessor currentUser) : PageModel
{
    /// <summary>Alle Einträge inkl. versteckter, sortiert wie sie aktuell wären.</summary>
    public List<NavRow> Rows { get; private set; } = new();

    public async Task OnGetAsync()
    {
        var ctx = await currentUser.GetAsync();
        var navCtx = new NavMenuContext(ctx.UserId, true);
        var comp = await composer.ComposeAsync(navCtx);
        var overrides = await db.NavMenuLayouts.AsNoTracking().ToDictionaryAsync(x => x.EntryKey);

        Rows = comp.All
            .Select(x => new NavRow(
                Key: x.Entry.Key,
                DefaultLabel: x.Entry.Label,
                DefaultSection: x.Entry.Section,
                DefaultSort: x.Entry.SortOrder,
                Icon: x.Entry.Icon,
                Url: x.Entry.Url,
                LabelOverride: overrides.GetValueOrDefault(x.Entry.Key)?.LabelOverride,
                SectionOverride: overrides.GetValueOrDefault(x.Entry.Key)?.SectionOverride,
                SortOverride: overrides.GetValueOrDefault(x.Entry.Key)?.SortOrderOverride,
                Hidden: x.Hidden))
            .OrderBy(r => r.SortOverride ?? r.DefaultSort)
            .ToList();
    }

    public async Task<IActionResult> OnPostSaveAsync(string[] entryKey, string?[] labelOverride,
        string?[] sectionOverride, string[] visible)
    {
        // Arrays sind in DOM-Reihenfolge der gespeicherten Tabelle.
        // Sortierung = (Index+1) × 10 — Schritte 10 erlauben dass neue Module
        // mit eigener SortOrder noch dazwischenrutschen können.
        //
        // „visible"-Checkbox liefert beide Werte ("false" Hidden-Input + "true"
        // wenn checked) — bei mehreren value-Submits behalten wir den letzten,
        // daher: per Index zwei Slots → Pro Row 2 Einträge im Array.
        var visiblePerRow = ParseVisibleArray(visible, entryKey.Length);

        for (int i = 0; i < entryKey.Length; i++)
        {
            var key = entryKey[i];
            var row = await db.NavMenuLayouts.FindAsync(new object[] { key });

            var label = string.IsNullOrWhiteSpace(labelOverride[i]) ? null : labelOverride[i]!.Trim();
            var section = sectionOverride[i] is null ? null : sectionOverride[i]!.Trim();
            var sortOverride = (i + 1) * 10;
            var isHidden = !visiblePerRow[i];

            if (row is null)
            {
                row = new NavMenuLayout { EntryKey = key };
                db.NavMenuLayouts.Add(row);
            }
            row.LabelOverride = label;
            row.SectionOverride = section;
            row.SortOrderOverride = sortOverride;
            row.IsHidden = isHidden;
            row.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();
        composer.InvalidateCache();
        TempData["StatusMessage"] = "Layout gespeichert.";
        return RedirectToPage();
    }

    /// <summary>
    /// Jede Row sendet 2 Slots für `visible`: Default "false" + optional "true" wenn checked.
    /// Wir lesen das Array in 2er-Schritten und nehmen das letzte "true" (falls vorhanden).
    /// </summary>
    private static bool[] ParseVisibleArray(string[] raw, int rowCount)
    {
        var result = new bool[rowCount];
        var perRow = raw.Length / Math.Max(1, rowCount);
        if (perRow <= 1)
        {
            // Fallback: nur 1 Wert pro Row
            for (int i = 0; i < rowCount && i < raw.Length; i++)
                result[i] = string.Equals(raw[i], "true", StringComparison.OrdinalIgnoreCase);
            return result;
        }
        for (int i = 0; i < rowCount; i++)
        {
            bool any = false;
            for (int j = 0; j < perRow; j++)
            {
                var idx = i * perRow + j;
                if (idx < raw.Length && string.Equals(raw[idx], "true", StringComparison.OrdinalIgnoreCase))
                    any = true;
            }
            result[i] = any;
        }
        return result;
    }

    public async Task<IActionResult> OnPostResetAsync(string key)
    {
        var row = await db.NavMenuLayouts.FindAsync(new object[] { key });
        if (row is not null)
        {
            db.NavMenuLayouts.Remove(row);
            await db.SaveChangesAsync();
            composer.InvalidateCache();
        }
        return RedirectToPage();
    }

    public record NavRow(
        string Key,
        string DefaultLabel,
        string? DefaultSection,
        int DefaultSort,
        string Icon,
        string Url,
        string? LabelOverride,
        string? SectionOverride,
        int? SortOverride,
        bool Hidden);
}
