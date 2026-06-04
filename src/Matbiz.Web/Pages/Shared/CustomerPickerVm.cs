namespace Matbiz.Web.Pages.Shared;

/// <summary>
/// Render parameters for the customer search dialog. The dialog itself lives
/// once per page; the <see cref="InputName"/> drives the hidden form-input
/// name so the value posts under the right model binding key.
/// </summary>
public record CustomerPickerVm(
    string DialogId,
    string InputName,
    Guid? SelectedId,
    string? SelectedLabel);
