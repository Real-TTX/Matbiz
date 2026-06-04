global using ThemeMode = Matbiz.Web.Modules.SystemSettings.Models.ThemeMode;
using System.ComponentModel.DataAnnotations;
using Matbiz.Web.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Matbiz.Web.Pages.Account;

[Authorize]
public class ManageModel(
    UserManager<ApplicationUser> users,
    SignInManager<ApplicationUser> signIn) : PageModel
{
    [BindProperty] public PasswordInput Password { get; set; } = new();
    [BindProperty] public ThemeMode Theme { get; set; }
    [BindProperty] public string? DisplayName { get; set; }

    [BindProperty(SupportsGet = true, Name = "tab")]
    public string? Tab { get; set; }

    public ApplicationUser? CurrentUser { get; private set; }
    public string ActiveTab => Tab?.ToLowerInvariant() == "appearance" ? "appearance" : "profile";

    public async Task<IActionResult> OnGetAsync()
    {
        CurrentUser = await users.GetUserAsync(User);
        if (CurrentUser is null) return Forbid();
        Theme = CurrentUser.Theme;
        DisplayName = CurrentUser.DisplayName;
        return Page();
    }

    public async Task<IActionResult> OnPostProfileAsync()
    {
        var user = await users.GetUserAsync(User);
        if (user is null) return Forbid();
        user.DisplayName = DisplayName;
        await users.UpdateAsync(user);
        TempData["StatusMessage"] = "Profil gespeichert.";
        return RedirectToPage(new { tab = "profile" });
    }

    public async Task<IActionResult> OnPostChangePasswordAsync()
    {
        if (!TryValidateModel(Password, nameof(Password)))
            return await OnGetAsync();
        var user = await users.GetUserAsync(User);
        if (user is null) return Forbid();

        var res = await users.ChangePasswordAsync(user, Password.Old, Password.New);
        if (!res.Succeeded)
        {
            foreach (var e in res.Errors) ModelState.AddModelError("", e.Description);
            return await OnGetAsync();
        }
        await signIn.RefreshSignInAsync(user);
        TempData["StatusMessage"] = "Passwort geändert.";
        return RedirectToPage(new { tab = "profile" });
    }

    public async Task<IActionResult> OnPostAppearanceAsync()
    {
        var user = await users.GetUserAsync(User);
        if (user is null) return Forbid();
        user.Theme = Theme;
        await users.UpdateAsync(user);
        TempData["StatusMessage"] = "Theme gespeichert.";
        return RedirectToPage(new { tab = "appearance" });
    }

    public class PasswordInput
    {
        [Required, DataType(DataType.Password)]
        public string Old { get; set; } = "";
        [Required, MinLength(8), DataType(DataType.Password)]
        public string New { get; set; } = "";
        [Required, Compare(nameof(New), ErrorMessage = "Passwörter stimmen nicht überein."), DataType(DataType.Password)]
        public string Confirm { get; set; } = "";
    }
}
