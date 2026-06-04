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

    [BindProperty] public NewUserDto NewUser { get; set; } = new();
    [BindProperty] public bool ShowCreate { get; set; }

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

    public static string Initials(string? source)
    {
        if (string.IsNullOrEmpty(source)) return "?";
        var clean = source.Contains('@') ? source[..source.IndexOf('@')] : source;
        var parts = clean.Split(new[] { ' ', '.', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 0 ? "?" :
            parts.Length == 1 ? parts[0][..1].ToUpper() :
            (parts[0][0].ToString() + parts[^1][0]).ToUpper();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        if (!ModelState.IsValid)
        {
            ShowCreate = true;
            await OnGetAsync();
            return Page();
        }

        var (res, _) = await userAdmin.CreateAsync(NewUser.Email, NewUser.DisplayName ?? "", NewUser.Password, NewUser.Role);
        if (!res.Succeeded)
        {
            foreach (var e in res.Errors) ModelState.AddModelError("", e.Description);
            ShowCreate = true;
            await OnGetAsync();
            return Page();
        }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleActiveAsync(string userId)
    {
        var u = (await userAdmin.ListAsync()).FirstOrDefault(x => x.Id == userId);
        if (u is not null) await userAdmin.SetActiveAsync(userId, !u.IsActive);
        return RedirectToPage();
    }

    public class NewUserDto
    {
        [Required, EmailAddress]
        public string Email { get; set; } = "";
        public string? DisplayName { get; set; }
        [Required, MinLength(8)]
        public string Password { get; set; } = "";
        public string Role { get; set; } = "User";
    }
}
