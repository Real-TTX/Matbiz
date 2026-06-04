using Matbiz.Web.Data;
using Matbiz.Web.Modules.Customers.Services;
using Matbiz.Web.Modules.Tasks.Models;
using Matbiz.Web.Modules.Tasks.Services;
using Matbiz.Web.Modules.Teams.Services;
using Matbiz.Web.Modules.Users.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TaskStatus = Matbiz.Web.Modules.Tasks.Models.TaskStatus;

namespace Matbiz.Web.Pages.Tasks;

[Authorize]
public class IndexModel(
    TaskService tasks,
    TeamService teams,
    CustomerService customers,
    UserAdminService userAdmin) : PageModel
{
    /// <summary>All tasks visible to the current user — assigned to them OR to a team they're in.</summary>
    public List<TaskItem> AllVisible { get; private set; } = new();

    public Dictionary<Guid, string> TeamNames { get; private set; } = new();
    public Dictionary<Guid, string> CustomerNames { get; private set; } = new();
    public Dictionary<string, string> UserDisplayById { get; private set; } = new();
    public string? CurrentUserId { get; private set; }

    [BindProperty(SupportsGet = true)] public string Scope { get; set; } = "all"; // all | mine | team
    [BindProperty(SupportsGet = true)] public string Status { get; set; } = "open"; // open | overdue | today | week | done | all
    [BindProperty(SupportsGet = true)] public TaskPriority? Priority { get; set; }
    [BindProperty(SupportsGet = true)] public string? Q { get; set; }

    public DateTime Today => DateTime.UtcNow.Date;

    public async Task<IActionResult> OnGetAsync()
    {
        var mine = await tasks.ListMineAsync();
        var team = await tasks.ListTeamAsync();
        // De-dupe: a task could in theory match both lists if assigned to a team the user is in.
        AllVisible = mine.Concat(team).DistinctBy(t => t.Id).ToList();

        CurrentUserId = (await userAdmin.ListAsync()).FirstOrDefault(u => u.UserName == User.Identity!.Name)?.Id;

        var teamIds = AllVisible.Where(t => t.AssignedTeamId is not null).Select(t => t.AssignedTeamId!.Value).Distinct().ToList();
        if (teamIds.Count > 0)
            TeamNames = (await teams.ListAsync()).Where(t => teamIds.Contains(t.Id)).ToDictionary(t => t.Id, t => t.Name);

        var customerIds = AllVisible.Where(t => t.CustomerId is not null).Select(t => t.CustomerId!.Value);
        CustomerNames = await customers.NamesByIdAsync(customerIds);

        UserDisplayById = (await userAdmin.ListAsync())
            .ToDictionary(u => u.Id, u => u.DisplayName ?? u.Email ?? u.Id);

        if (Request.Headers.ContainsKey("HX-Request"))
            return Partial("_TaskTable", this);

        return Page();
    }

    public IEnumerable<TaskItem> Filtered
    {
        get
        {
            IEnumerable<TaskItem> q = AllVisible;

            // Scope: mine vs. team-only vs. all
            q = Scope switch
            {
                "mine" => q.Where(t => t.AssignedUserId == CurrentUserId),
                "team" => q.Where(t => t.AssignedTeamId is not null),
                _ => q
            };

            q = Status switch
            {
                "open" => q.Where(t => t.Status != TaskStatus.Done && t.Status != TaskStatus.Cancelled),
                "overdue" => q.Where(IsOverdue),
                "today" => q.Where(t => t.Status != TaskStatus.Done && t.DueDate?.Date == Today),
                "week" => q.Where(t => t.Status != TaskStatus.Done && t.DueDate is DateTime d && d.Date >= Today && d.Date <= Today.AddDays(7)),
                "done" => q.Where(t => t.Status == TaskStatus.Done),
                _ => q
            };

            if (Priority is TaskPriority p)
                q = q.Where(t => t.Priority == p);

            if (!string.IsNullOrWhiteSpace(Q))
            {
                var s = Q.Trim();
                q = q.Where(t =>
                    (t.Title?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false)
                    || (t.Description?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            return q.OrderBy(t => t.Status).ThenByDescending(t => t.Priority).ThenBy(t => t.DueDate ?? DateTime.MaxValue);
        }
    }

    public int OverdueCount => AllVisible.Count(IsOverdue);
    public int TodayCount => AllVisible.Count(t => t.Status != TaskStatus.Done && t.DueDate?.Date == Today);
    public int OpenCount => AllVisible.Count(t => t.Status != TaskStatus.Done && t.Status != TaskStatus.Cancelled);

    public bool IsOverdue(TaskItem t) =>
        t.Status != TaskStatus.Done && t.Status != TaskStatus.Cancelled
        && t.DueDate is DateTime d && d.Date < Today;

    public string AssigneeLabel(TaskItem t)
    {
        if (t.AssignedTeamId is Guid tid && TeamNames.TryGetValue(tid, out var teamName))
            return "Team: " + teamName;
        if (!string.IsNullOrEmpty(t.AssignedUserId) && UserDisplayById.TryGetValue(t.AssignedUserId, out var n))
            return n;
        return "—";
    }
}
