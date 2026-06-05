using System.ComponentModel.DataAnnotations;

namespace Matbiz.Web.Modules.CustomFields.Models;

/// <summary>
/// Gruppiert <see cref="CustomFieldDefinition"/>s zu einem Block / Tab.
/// Sektionen sind pro EntityType getrennt — eine „Service-Daten"-Sektion
/// auf Artikeln ist nicht dieselbe wie auf Kontakten.
/// </summary>
public class CustomFieldSection
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public CustomFieldEntityType EntityType { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<CustomFieldDefinition> Fields { get; set; } = new();
}
