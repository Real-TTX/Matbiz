using Matbiz.Web.Data;
using Matbiz.Web.Modules.Customers.Services;
using Matbiz.Web.Modules.Tasks.Models;
using Matbiz.Web.Modules.Tasks.Services;
using Matbiz.Web.Modules.Teams.Models;
using Matbiz.Web.Modules.Teams.Services;
using Matbiz.Web.Modules.Users.Services;
using Matbiz.Web.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Matbiz.Web.Pages.Tasks;

[Authorize]
public class EditModel(
    TaskService tasks,
    TeamService teamSvc,
    UserAdminService userAdmin,
    CustomerService customers,
    ICurrentUserAccessor current) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public Guid? Id { get; set; }

    [BindProperty(SupportsGet = true, Name = "customerId")]
    public Guid? CustomerIdFromQuery { get; set; }

    [BindProperty] public TaskItem Input { get; set; } = new();
    [BindProperty] public string AssignmentKey { get; set; } = "self";

    public bool IsNew => Id is null;
    public string? CurrentUserId { get; private set; }
    public List<ApplicationUser> Users { get; private set; } = new();
    public List<Team> MyTeams { get; private set; } = new();
    public List<(Guid Id, string Label)> Customers { get; private set; } = new();

    public string? SelectedCustomerLabel { get; private set; }
    public Dictionary<string, ApplicationUser> UsersById { get; private set; } = new();

    public string ActorName(string userId) =>
        UsersById.TryGetValue(userId, out var u) ? (u.DisplayName ?? u.Email ?? userId) : userId;

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadLookupsAsync();
        if (Id is Guid gid)
        {
            var t = await tasks.GetAsync(gid);
            if (t is null) return NotFound();
            Input = t;
            AssignmentKey = Input.AssignedTeamId is Guid teamId ? $"team:{teamId}"
                : Input.AssignedUserId is { } uid && uid != CurrentUserId ? $"user:{uid}"
                : "self";
        }
        else if (CustomerIdFromQuery is Guid cid)
        {
            Input.CustomerId = cid;
        }
        return Page();
    }

    private async Task LoadLookupsAsync()
    {
        var ctx = await current.GetAsync();
        CurrentUserId = ctx.UserId;
        Users = await userAdmin.ListAsync();
        UsersById = Users.ToDictionary(u => u.Id, u => u);
        MyTeams = CurrentUserId is null ? new() : await teamSvc.ListForUserAsync(CurrentUserId);
        Customers = new();
        if (Input.CustomerId is Guid cid)
        {
            var names = await customers.NamesByIdAsync(new[] { cid });
            names.TryGetValue(cid, out var lbl);
            SelectedCustomerLabel = lbl;
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadLookupsAsync();
        if (string.IsNullOrWhiteSpace(Input.Title))
        {
            ModelState.AddModelError("Input.Title", "Titel ist erforderlich.");
            return Page();
        }

        ApplyAssignment();

        if (IsNew)
        {
            await tasks.CreateAsync(Input);
        }
        else
        {
            Input.Id = Id!.Value;
            await tasks.UpdateAsync(Input);
        }
        return RedirectToPage("/Tasks/Index");
    }

    public async Task<IActionResult> OnPostDeleteAsync()
    {
        if (Id is Guid gid) await tasks.DeleteAsync(gid);
        return RedirectToPage("/Tasks/Index");
    }

    private void ApplyAssignment()
    {
        if (AssignmentKey.StartsWith("team:") && Guid.TryParse(AssignmentKey[5..], out var tid))
        {
            Input.AssignedTeamId = tid;
            Input.AssignedUserId = null;
        }
        else if (AssignmentKey.StartsWith("user:"))
        {
            Input.AssignedUserId = AssignmentKey[5..];
            Input.AssignedTeamId = null;
        }
        else
        {
            Input.AssignedUserId = CurrentUserId;
            Input.AssignedTeamId = null;
        }
    }
}
