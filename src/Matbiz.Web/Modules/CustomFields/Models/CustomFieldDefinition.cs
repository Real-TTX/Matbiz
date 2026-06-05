using System.ComponentModel.DataAnnotations;

namespace Matbiz.Web.Modules.CustomFields.Models;

/// <summary>
/// Definition eines Custom-Felds, polymorph über alle Entity-Typen.
/// Eindeutigkeit von <see cref="Key"/> ist pro EntityType (selber Key
/// kann auf Kontakt und Artikel parallel existieren).
/// </summary>
public class CustomFieldDefinition
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public CustomFieldEntityType EntityType { get; set; }

    [Required, MaxLength(100)]
    public string Key { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Label { get; set; } = string.Empty;

    public CustomFieldType Type { get; set; } = CustomFieldType.Text;

    public bool Required { get; set; }

    public int SortOrder { get; set; }

    public Guid? SectionId { get; set; }
    public CustomFieldSection? Section { get; set; }

    /// <summary>Für Type=ValueList: zulässige Werte, eine pro Zeile.</summary>
    public string? Options { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public IEnumerable<string> GetOptions() =>
        string.IsNullOrWhiteSpace(Options)
            ? Array.Empty<string>()
            : Options.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
