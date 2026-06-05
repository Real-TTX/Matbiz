using Matbiz.Web.Shared.Navigation;

namespace Matbiz.Web.Modules.Tasks.Navigation;

/// <summary>
/// Demonstriert: ein Modul kann beliebig viele Einträge anlegen.
/// Hauptpunkt „Aufgaben" + 3 Quick-Filter als Sub-Einträge mit vorbelegten Query-Params.
/// Admin kann jeden Eintrag einzeln im Menü-Layout aus/einblenden oder umbenennen.
/// </summary>
public class TasksNavMenuProvider : INavMenuProvider
{
    public string? ModuleKey => "Tasks";

    public Task<IReadOnlyList<NavMenuEntry>> GetEntriesAsync(NavMenuContext ctx, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<NavMenuEntry>>(new[]
        {
            new NavMenuEntry("tasks",          "Aufgaben",     "bi-check2-square",        "/Tasks",                          SortOrder: 20, ActiveOnPrefix: "/Tasks"),

            // Default versteckt — Admin schaltet je nach Geschmack frei.
            new NavMenuEntry("tasks:overdue",  "Überfällig",   "bi-exclamation-triangle", "/Tasks?status=overdue",           SortOrder: 21, IsSub: true, HiddenByDefault: true),
            new NavMenuEntry("tasks:today",    "Heute fällig", "bi-calendar-day",         "/Tasks?status=today",             SortOrder: 22, IsSub: true, HiddenByDefault: true),
            new NavMenuEntry("tasks:mine",     "Meine offenen","bi-person-check",         "/Tasks?scope=mine&status=open",   SortOrder: 23, IsSub: true, HiddenByDefault: true),
        });
}
