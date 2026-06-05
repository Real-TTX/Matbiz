namespace Matbiz.Web.Modules.Modules.Models;

/// <summary>
/// Statische Definition eines App-Moduls. Module sind die großen Feature-Brocken,
/// die der Admin pro Mandant aktivieren/deaktivieren kann.
///
/// IsCore=true bedeutet: das Modul ist Basis und kann NICHT deaktiviert werden
/// (User-Login, Kontakte, Branding usw. — ohne die ist die App kaputt).
/// </summary>
public record ModuleDefinition(
    string Key,
    string Name,
    string Description,
    string Icon,
    bool IsCore,
    int SortOrder);

public class ModuleSetting
{
    public string Key { get; set; } = string.Empty;  // Primary Key
    public bool IsEnabled { get; set; } = true;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
