namespace Matbiz.Web.Modules.CustomFields.Models;

/// <summary>
/// Wert eines <see cref="CustomFieldDefinition"/> für eine konkrete Entity.
/// <see cref="EntityId"/> ist polymorph — Foreign-Key wird app-seitig
/// gehalten (kein DB-FK), weil das Ziel je nach EntityType eine andere
/// Tabelle ist (Customers / Articles / …).
/// </summary>
public class CustomFieldValue
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public CustomFieldEntityType EntityType { get; set; }

    /// <summary>Id der Entity (Customer, Article, …) — polymorph.</summary>
    public Guid EntityId { get; set; }

    public Guid FieldDefinitionId { get; set; }
    public CustomFieldDefinition FieldDefinition { get; set; } = default!;

    public string? Value { get; set; }
}
