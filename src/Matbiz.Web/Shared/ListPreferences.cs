using System.Text.Json;

namespace Matbiz.Web.Shared;

public record ColumnDef(string Key, string Label, bool DefaultVisible = true);

public class ColumnConfig
{
    public required string ListKey { get; init; }
    public required IReadOnlyList<ColumnDef> All { get; init; }
    public required HashSet<string> Visible { get; init; }

    public bool Is(string key) => Visible.Contains(key);
}

internal class ListPrefRow
{
    public List<string> Visible { get; set; } = new();
    /// <summary>All column keys that existed when the user last saved this list's prefs.
    /// Used to decide whether a missing key is "explicitly hidden" or "didn't exist yet".</summary>
    public List<string> Known { get; set; } = new();
}

public static class ListPreferences
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = false };

    public static ColumnConfig Resolve(string? json, string listKey, IReadOnlyList<ColumnDef> all)
    {
        Dictionary<string, ListPrefRow>? store = null;
        if (!string.IsNullOrWhiteSpace(json))
        {
            try { store = JsonSerializer.Deserialize<Dictionary<string, ListPrefRow>>(json, Json); }
            catch { /* fall through to defaults */ }
        }

        if (store is not null && store.TryGetValue(listKey, out var row) && row.Visible.Count > 0)
        {
            var visible = row.Visible.ToHashSet();
            var known = row.Known?.ToHashSet() ?? new HashSet<string>();

            // Columns added since the user last saved: if they default to visible
            // AND the user has never seen them, opt them in automatically. This
            // is how new default columns appear for established users without
            // them needing to "Reset".
            foreach (var col in all)
                if (col.DefaultVisible && !known.Contains(col.Key) && known.Count > 0)
                    visible.Add(col.Key);

            // Drop keys that no longer exist in the column set.
            visible.IntersectWith(all.Select(c => c.Key));
            return new ColumnConfig { ListKey = listKey, All = all, Visible = visible };
        }

        return new ColumnConfig
        {
            ListKey = listKey,
            All = all,
            Visible = all.Where(c => c.DefaultVisible).Select(c => c.Key).ToHashSet()
        };
    }

    public static string Save(string? existingJson, string listKey, IEnumerable<string> visibleKeys, IEnumerable<string> allKnownKeys)
    {
        var store = new Dictionary<string, ListPrefRow>();
        if (!string.IsNullOrWhiteSpace(existingJson))
        {
            try { store = JsonSerializer.Deserialize<Dictionary<string, ListPrefRow>>(existingJson, Json) ?? new(); }
            catch { store = new(); }
        }
        store[listKey] = new ListPrefRow
        {
            Visible = visibleKeys.ToList(),
            Known = allKnownKeys.ToList()
        };
        return JsonSerializer.Serialize(store, Json);
    }
}
