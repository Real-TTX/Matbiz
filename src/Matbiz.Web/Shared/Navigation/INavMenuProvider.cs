namespace Matbiz.Web.Shared.Navigation;

/// <summary>
/// Plugin-Hook: jedes Modul registriert seine eigenen Sidebar-Einträge.
/// Composer sammelt sie, wendet Admin-Layout-Overrides an, sortiert sie und
/// gruppiert nach Section. Tools-Sammelbecken existiert nicht mehr — alles
/// läuft über Provider, auch die admin-verwalteten Custom-Links.
/// </summary>
public interface INavMenuProvider
{
    /// <summary>Bezug zum ModuleRegistry-Key. Ist der Modul-Toggle aus, werden die
    /// Einträge unterdrückt. <c>null</c> = immer anzeigen (Core).</summary>
    string? ModuleKey { get; }

    Task<IReadOnlyList<NavMenuEntry>> GetEntriesAsync(NavMenuContext ctx, CancellationToken ct = default);
}

/// <summary>Kontext für den Provider — User, Admin-Status etc.</summary>
public record NavMenuContext(string? UserId, bool IsAdmin);

/// <summary>
/// Ein Sidebar-Eintrag. <see cref="Key"/> ist die stabile ID für Layout-Overrides
/// (in der NavMenuLayout-Tabelle).
/// </summary>
public record NavMenuEntry(
    string Key,
    string Label,
    string Icon,
    string Url,
    string? Section = null,
    int SortOrder = 100,
    bool IsSub = false,
    string? ActiveOnPrefix = null,
    bool OpenInNewTab = false,
    /// <summary>
    /// Wenn true: Eintrag ist initial unsichtbar — Admin kann ihn über
    /// `/Admin/NavLayout` einblenden. Nützlich für Spezial-Quick-Filter,
    /// die nicht jeder dauerhaft braucht.
    /// </summary>
    bool HiddenByDefault = false);
