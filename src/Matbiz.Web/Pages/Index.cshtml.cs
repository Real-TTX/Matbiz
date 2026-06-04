using Matbiz.Web.Data;
using Matbiz.Web.Modules.Customers.Models;
using Matbiz.Web.Modules.Customers.Services;
using Matbiz.Web.Modules.Dashboard;
using Matbiz.Web.Modules.Tasks.Models;
using Matbiz.Web.Modules.Tasks.Services;
using Matbiz.Web.Modules.Users.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TaskStatus = Matbiz.Web.Modules.Tasks.Models.TaskStatus;

namespace Matbiz.Web.Pages;

[Authorize]
public class IndexModel(
    TaskService tasks,
    CustomerService customers,
    CompanyService companies,
    UserManager<ApplicationUser> users) : PageModel
{
    public List<TaskItem> Mine { get; private set; } = new();
    public List<TaskItem> Team { get; private set; } = new();
    public Dictionary<Guid, string> CustomerNames { get; private set; } = new();
    public List<Customer> NewContacts { get; private set; } = new();
    public List<Company> NewCompanies { get; private set; } = new();
    public List<CustomerHistoryEntry> RecentHistory { get; private set; } = new();
    public DashboardConfig Config { get; private set; } = DashboardConfig.Default();

    public DateTime Today => DateTime.UtcNow.Date;
    public DateTime WeekEnd => DateTime.UtcNow.Date.AddDays(7);

    public List<TaskItem> Open => Mine.Where(t => t.Status != TaskStatus.Done && t.Status != TaskStatus.Cancelled).ToList();
    public List<TaskItem> Overdue => Open.Where(t => t.DueDate is DateTime d && d.Date < Today).OrderBy(t => t.DueDate).ToList();
    public List<TaskItem> DueToday => Open.Where(t => t.DueDate?.Date == Today).OrderByDescending(t => t.Priority).ToList();
    public List<TaskItem> Week => Open.Where(t => t.DueDate is DateTime d && d.Date > Today && d.Date <= WeekEnd).OrderBy(t => t.DueDate).ToList();
    public List<TaskItem> TeamUpcoming => Team
        .Where(t => t.Status != TaskStatus.Done && t.Status != TaskStatus.Cancelled)
        .OrderBy(t => t.DueDate ?? DateTime.MaxValue).ToList();

    public int OverdueCount => Overdue.Count;
    public int TodayCount => DueToday.Count;
    public int WeekCount => Week.Count;
    public int OpenCount => Open.Count;
    public int TeamOpenCount => Team.Count(t => t.Status != TaskStatus.Done && t.Status != TaskStatus.Cancelled);

    public string FirstName
    {
        get
        {
            var n = User.Identity?.Name ?? "";
            if (string.IsNullOrEmpty(n)) return "";
            return n.Contains('@') ? n[..n.IndexOf('@')] : n.Split(' ')[0];
        }
    }

    public async Task OnGetAsync()
    {
        var user = await users.GetUserAsync(User);
        Config = DashboardConfig.Load(user?.DashboardConfigJson);

        Mine = await tasks.ListMineAsync();
        Team = await tasks.ListTeamAsync();

        var ids = Mine.Concat(Team).Where(t => t.CustomerId is not null).Select(t => t.CustomerId!.Value);
        CustomerNames = await customers.NamesByIdAsync(ids);

        // Two new widgets — only fetch if enabled to skip wasted DB roundtrips.
        var contactsWidget = Config.Widgets.FirstOrDefault(w => w.Type == DashboardWidget.NewContacts);
        if (contactsWidget is { Enabled: true })
            NewContacts = await customers.RecentlyCreatedAsync(Math.Max(1, contactsWidget.MaxItems));

        var companiesWidget = Config.Widgets.FirstOrDefault(w => w.Type == DashboardWidget.NewCompanies);
        if (companiesWidget is { Enabled: true })
            NewCompanies = await companies.RecentlyCreatedAsync(Math.Max(1, companiesWidget.MaxItems));

        var historyWidget = Config.Widgets.FirstOrDefault(w => w.Type == DashboardWidget.RecentHistory);
        if (historyWidget is { Enabled: true })
            RecentHistory = await customers.RecentHistoryAsync(Math.Max(1, historyWidget.MaxItems));
    }

    public async Task<IActionResult> OnPostSaveConfigAsync([FromForm] DashboardSubmitDto dto)
    {
        var user = await users.GetUserAsync(User);
        if (user is null) return Forbid();

        var cfg = new DashboardConfig
        {
            Widgets = dto.Widgets
                .OrderBy(w => w.Order)
                .Select(w => new WidgetConfig
                {
                    Type = w.Type,
                    Enabled = w.Enabled,
                    Order = w.Order,
                    MaxItems = Math.Clamp(w.MaxItems, 1, 50)
                }).ToList()
        };
        user.DashboardConfigJson = DashboardConfig.Save(cfg);
        await users.UpdateAsync(user);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostMoveAsync(DashboardWidget type, int delta)
    {
        var user = await users.GetUserAsync(User);
        if (user is null) return Forbid();
        var cfg = DashboardConfig.Load(user.DashboardConfigJson);
        var sorted = cfg.Widgets.OrderBy(w => w.Order).ToList();
        var idx = sorted.FindIndex(w => w.Type == type);
        var newIdx = idx + delta;
        if (idx < 0 || newIdx < 0 || newIdx >= sorted.Count) return RedirectToPage();
        (sorted[idx], sorted[newIdx]) = (sorted[newIdx], sorted[idx]);
        for (var i = 0; i < sorted.Count; i++) sorted[i].Order = i;
        cfg.Widgets = sorted;
        user.DashboardConfigJson = DashboardConfig.Save(cfg);
        await users.UpdateAsync(user);
        return RedirectToPage();
    }

    public class DashboardSubmitDto
    {
        public List<WidgetRow> Widgets { get; set; } = new();
    }

    public class WidgetRow
    {
        public DashboardWidget Type { get; set; }
        public bool Enabled { get; set; }
        public int Order { get; set; }
        public int MaxItems { get; set; } = 8;
    }
}
