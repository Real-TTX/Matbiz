using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Matbiz.Web.Shared;

/// <summary>
/// Breadcrumbs werden per Default automatisch aus dem URL-Pfad abgeleitet
/// (siehe <see cref="AutoDerive"/>). Pages mit komplexer Navigation können
/// das per <see cref="Set"/> überschreiben — z.B. um Entity-Namen in den Pfad
/// einzubauen wie „Kontakte › Max Mustermann › Bearbeiten".
/// </summary>
public static class BreadcrumbsHelper
{
    const string KEY = "__matbiz_breadcrumbs__";

    public record Crumb(string Label, string? Url = null);

    public static void Set(ViewDataDictionary vd, params Crumb[] crumbs) => vd[KEY] = crumbs.ToList();
    public static void Set(ViewDataDictionary vd, IEnumerable<Crumb> crumbs) => vd[KEY] = crumbs.ToList();
    public static List<Crumb>? Get(ViewDataDictionary vd) => vd[KEY] as List<Crumb>;

    private static readonly Dictionary<string, string> SegmentLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["customers"] = "Kontakte",
        ["companies"] = "Firmen",
        ["tasks"] = "Aufgaben",
        ["wiki"] = "Wiki",
        ["users"] = "Benutzer",
        ["teams"] = "Teams",
        ["departments"] = "Abteilungen",
        ["admin"] = "Administration",
        ["menu"] = "Menü-Einträge",
        ["system"] = "System",
        ["tools"] = "Tools",
        ["account"] = "Mein Konto",
        ["fields"] = "Kontaktfelder",
        ["groups"] = "Gruppen",
        ["edit"] = "Bearbeiten",
        ["editfield"] = "Feld bearbeiten",
        ["editsection"] = "Sektion bearbeiten",
        ["create"] = "Anlegen",
        ["detail"] = "Detail",
        ["appearance"] = "Aussehen",
        ["manage"] = "Profil",
        ["view"] = "Ansicht",
    };

    /// <summary>
    /// Generiert eine Breadcrumb-Kette aus URL-Segmenten. Letzter Eintrag = aktuelle Seite,
    /// nutzt <paramref name="title"/> wenn das letzte Segment nicht erkannt wird
    /// (z.B. ein Slug oder eine Guid).
    /// </summary>
    public static List<Crumb> AutoDerive(string path, string? title)
    {
        var list = new List<Crumb>();

        var segments = path.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0)
        {
            // Root = Dashboard
            return new() { new(title ?? "Dashboard") };
        }

        // Spezialfall: einige Pfade haben zwar einen Customers/-Präfix,
        // sind aber eigenständige Sidebar-Einträge — der „Kontakte"-Crumb
        // wäre also falsch, weil im Menü links nichts hervorgehoben wird.
        var hiddenPrefix = "";
        if (segments.Length >= 2
            && string.Equals(segments[0], "Customers", StringComparison.OrdinalIgnoreCase)
            && (string.Equals(segments[1], "Groups", StringComparison.OrdinalIgnoreCase)
                || string.Equals(segments[1], "Fields", StringComparison.OrdinalIgnoreCase)))
        {
            hiddenPrefix = "/" + segments[0];
            segments = segments.Skip(1).ToArray();
        }

        var accPath = hiddenPrefix;
        for (int i = 0; i < segments.Length; i++)
        {
            var seg = segments[i];
            accPath += "/" + seg;

            // Route-Werte überspringen (GUIDs, Zahlen, klassische Slug-Marker)
            if (Guid.TryParse(seg, out _) || int.TryParse(seg, out _)) continue;

            var label = SegmentLabels.TryGetValue(seg, out var l) ? l : Capitalize(seg);
            bool isLast = i == segments.Length - 1;

            // Bei letztem Segment: wenn Title gesetzt und nicht trivial = Title nehmen
            if (isLast && !string.IsNullOrEmpty(title) && !string.Equals(label, title, StringComparison.OrdinalIgnoreCase))
            {
                list.Add(new(title));
            }
            else
            {
                list.Add(new(label, isLast ? null : accPath));
            }
        }

        // Falls letztes Segment GUID war → Title als letzter Crumb anhängen
        if (list.Count > 0 && list[^1].Url != null && !string.IsNullOrEmpty(title))
        {
            list.Add(new(title));
        }

        return list;
    }

    private static string Capitalize(string s) => s.Length switch
    {
        0 => s,
        1 => s.ToUpperInvariant(),
        _ => char.ToUpperInvariant(s[0]) + s[1..]
    };
}
