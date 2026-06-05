using System.ComponentModel.DataAnnotations;

namespace Matbiz.Web.Modules.Modules.Models;

/// <summary>
/// Admin-Override für die Sidebar — pro Eintrag (identifiziert über stabilen Key)
/// kann Label, Section und Sortierung umgebogen werden, oder der Eintrag wird
/// versteckt. Wenn kein Override vorhanden: Provider-Default zählt.
/// </summary>
public class NavMenuLayout
{
    [Key, MaxLength(100)]
    public string EntryKey { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? LabelOverride { get; set; }

    /// <summary>Leerer String = explizit „keine Sektion".</summary>
    [MaxLength(100)]
    public string? SectionOverride { get; set; }

    public int? SortOrderOverride { get; set; }

    public bool IsHidden { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
