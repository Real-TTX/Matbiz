namespace Matbiz.Web.Modules.CustomFields.Models;

/// <summary>
/// An welcher Entity-Art hängen die Custom-Fields?
/// Stabile Integer-Werte — werden in DB persistiert.
/// </summary>
public enum CustomFieldEntityType
{
    Contact = 0,
    Article = 1,
    Company = 2,   // Reserviert für später
    Task    = 3,   // Reserviert für später
}
