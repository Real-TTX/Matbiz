namespace Matbiz.Web.Pages.Shared;

/// <summary>
/// Generic search-dialog picker parameters. The same dialog UI is reused for
/// contacts and companies; <see cref="SearchUrl"/> points at the partial-only
/// endpoint that returns matching rows.
/// </summary>
public record EntityPickerVm(
    string DialogId,
    string InputName,
    Guid? SelectedId,
    string? SelectedLabel,
    string SearchUrl,
    string EmptyText,
    string DialogTitle,
    string PlaceholderText,
    string IconClass);
