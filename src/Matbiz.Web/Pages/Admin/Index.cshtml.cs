using Matbiz.Web.Data;
using Matbiz.Web.Modules.CustomMenu.Services;
using Matbiz.Web.Modules.Teams.Services;
using Matbiz.Web.Modules.Users.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Matbiz.Web.Pages.Admin;

[Authorize(Roles = "Admin")]
public class IndexModel(
    UserManager<ApplicationUser> users,
    TeamService teams,
    DepartmentService departments,
    CustomMenuService customMenu) : PageModel
{
    public int UserCount { get; private set; }
    public int ActiveUserCount { get; private set; }
    public int AdminCount { get; private set; }
    public int TeamCount { get; private set; }
    public int DepartmentCount { get; private set; }
    public int CustomMenuItemCount { get; private set; }

    public async Task OnGetAsync()
    {
        var allUsers = users.Users.ToList();
        UserCount = allUsers.Count;
        ActiveUserCount = allUsers.Count(u => u.IsActive);

        AdminCount = 0;
        foreach (var u in allUsers)
            if (await users.IsInRoleAsync(u, Roles.Admin))
                AdminCount++;

        TeamCount = (await teams.ListAsync()).Count;
        DepartmentCount = (await departments.ListAsync()).Count;
        CustomMenuItemCount = (await customMenu.ListAllAsync()).Count;
    }
}
