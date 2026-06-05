using System.ComponentModel.DataAnnotations;
using Matbiz.Web.Data;
using Matbiz.Web.Modules.Users.Services;
using Matbiz.Web.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Matbiz.Web.Pages.Users;

[Authorize(Roles = "Admin")]
public class IndexModel(
    UserAdminService userAdmin,
    UserManager<ApplicationUser> userManager,
    ICurrentUserAccessor current) : PageModel
{
    public List<ApplicationUser> Items { get; private set; } = new();
    public Dictionary<string, IList<string>> RolesByUserId { get; private set; } = new();
    public string? CurrentUserId { get; private set; }

    public async Task OnGetAsync()
    {
        var ctx = await current.GetAsync();
        CurrentUserId = ctx.ImpersonatorId ?? ctx.UserId;
        Items = await userAdmin.ListAsync();
        foreach (var u in Items)
            RolesByUserId[u.Id] = await userManager.GetRolesAsync(u);
    }

    public string PrimaryRole(string userId) =>
        RolesByUserId.TryGetValue(userId, out var roles) && roles.Count > 0 ? roles[0] : "User";
}
