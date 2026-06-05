namespace Matbiz.Web.Modules.CustomFields.Models;

/// <summary>
/// Datentyp eines Custom-Felds. Wertspeicherung erfolgt immer als String —
/// hier nur die Render- und Validierungs-Hint.
/// </summary>
public enum CustomFieldType
{
    Text = 0,
    Number = 1,
    Date = 2,
    Boolean = 3,
    LongText = 4,
    File = 5,
    ValueList = 6
}
