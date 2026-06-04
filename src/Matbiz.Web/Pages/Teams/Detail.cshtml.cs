using Matbiz.Web.Data;
using Matbiz.Web.Modules.Tasks.Models;
using Matbiz.Web.Modules.Tasks.Services;
using Matbiz.Web.Modules.Teams.Models;
using Matbiz.Web.Modules.Teams.Services;
using Matbiz.Web.Modules.Users.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Matbiz.Web.Pages.Teams;

[Authorize(Roles = "Admin")]
public class DetailModel(
    TeamService teams,
    UserAdminService userAdmin,
    TaskService tasks,
    DepartmentService departments) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    public Team? Team { get; private set; }
    public List<ApplicationUser> AllUsers { get; private set; } = new();
    public List<TaskItem> TeamTasks { get; private set; } = new();
    public List<Matbiz.Web.Modules.Teams.Models.Department> AllDepartments { get; private set; } = new();

    [BindProperty] public string? AddUserId { get; set; }
    [BindProperty] public string? Name { get; set; }
    [BindProperty] public string? Description { get; set; }
    [BindProperty] public Guid? DepartmentId { get; set; }

    public List<ApplicationUser> AvailableUsers =>
        AllUsers.Where(u => u.IsActive && Team is not null && !Team.Members.Any(m => m.UserId == u.Id)).ToList();

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadAsync();
        if (Team is null) return NotFound();
        return Page();
    }

    private async Task LoadAsync()
    {
        Team = await teams.GetAsync(Id);
        AllUsers = await userAdmin.ListAsync();
        AllDepartments = await departments.ListAsync();
        if (Team is not null)
        {
            TeamTasks = await tasks.ListByTeamAsync(Team.Id);
            Name = Team.Name;
            Description = Team.Description;
            DepartmentId = Team.DepartmentId;
        }
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        Team = await teams.GetAsync(Id);
        if (Team is null) return NotFound();
        Team.Name = Name ?? Team.Name;
        Team.Description = Description;
        Team.DepartmentId = DepartmentId;
        await teams.UpdateAsync(Team);
        TempData["StatusMessage"] = "Team gespeichert.";
        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostAddMemberAsync()
    {
        if (!string.IsNullOrEmpty(AddUserId))
            await teams.AddMemberAsync(Id, AddUserId);
        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostRemoveMemberAsync(string userId)
    {
        await teams.RemoveMemberAsync(Id, userId);
        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostDeleteAsync()
    {
        await teams.DeleteAsync(Id);
        return RedirectToPage("/Teams/Index");
    }
}
