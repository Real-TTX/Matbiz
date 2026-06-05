namespace Matbiz.Web.Shared.Actions;

/// <summary>
/// Cross-Modul-Plugin-Hook: ein Modul registriert Aktionen, die in einem anderen
/// Modul (z.B. Kontakt-Detail, Firma-Detail) als Button erscheinen — ohne dass
/// das andere Modul davon weiß. Anti-Spaghetti-Mechanismus.
///
/// Beispiel: Documents-Modul registriert „Rechnung erstellen" → erscheint
/// automatisch auf jedem Kontakt-Detail, wenn das Modul aktiv ist.
///
/// Provider werden via DI mehrfach für IEntityActionProvider registriert
/// (<c>builder.Services.AddScoped&lt;IEntityActionProvider, MyProvider&gt;()</c>).
/// Die Verbraucher-Seite (Kontakt-Detail etc.) injiziert <c>IEnumerable&lt;IEntityActionProvider&gt;</c>
/// und filtert nach <see cref="EntityActionContext.EntityType"/>.
/// </summary>
public interface IEntityActionProvider
{
    /// <summary>Liefert Aktionen für den gegebenen Kontext oder leere Liste.</summary>
    Task<IReadOnlyList<EntityAction>> GetAsync(EntityActionContext ctx, CancellationToken ct = default);
}

/// <summary>
/// Wer ruft mich an? Welche Entity, mit welcher Id, welcher User.
/// </summary>
public record EntityActionContext(
    string EntityType,    // "Contact" / "Company" / ...
    Guid EntityId,
    string? UserId);

/// <summary>
/// Eine konkrete Aktion: Label, Icon, Ziel-URL. Wird als Button gerendert.
/// </summary>
public record EntityAction(
    string Label,
    string Icon,
    string Url,
    string? Tooltip = null,
    string ButtonClass = "btn-outline-secondary",
    int SortOrder = 100,
    string? Group = null);   // optional: für Gruppierung im Dropdown

/// <summary>Übliche Werte für EntityType-Strings.</summary>
public static class EntityTypes
{
    public const string Contact = "Contact";
    public const string Company = "Company";
}
