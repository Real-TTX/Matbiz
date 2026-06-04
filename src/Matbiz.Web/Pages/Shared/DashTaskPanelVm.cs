using Matbiz.Web.Modules.Tasks.Models;

namespace Matbiz.Web.Pages.Shared;

public record DashTaskPanelVm(
    string Title,
    string IconClass,
    List<TaskItem> Items,
    string EmptyMessage,
    DateTime Today,
    Dictionary<Guid, string> CustomerNames,
    string AllUrl);
