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
public class EditModel(
    UserAdminService userAdmin,
    UserManager<ApplicationUser> userManager,
    ICurrentUserAccessor current) : PageModel
{
    [BindProperty(SupportsGet = true)] public string? Id { get; set; }

    [BindProperty, Required, EmailAddress] public string Email { get; set; } = "";
    [BindProperty] public string? DisplayName { get; set; }
    [BindProperty] public string Role { get; set; } = "User";
    [BindProperty] public bool IsActive { get; set; } = true;
    [BindProperty] public string? NewPassword { get; set; }

    public bool IsNew => string.IsNullOrEmpty(Id);
    public bool IsSelf { get; private set; }
    public string? CurrentRole { get; private set; }
    public ApplicationUser? Target { get; private set; }

    private async Task<string?> CurrentUserIdAsync()
    {
        var ctx = await current.GetAsync();
        // Bei Impersonation: der echte Admin (ImpersonatorId), nicht der „simulierte" User.
        return ctx.ImpersonatorId ?? ctx.UserId;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!IsNew)
        {
            Target = await userManager.FindByIdAsync(Id!);
            if (Target is null) return NotFound();
            Email = Target.Email ?? "";
            DisplayName = Target.DisplayName;
            IsActive = Target.IsActive;
            CurrentRole = (await userManager.GetRolesAsync(Target)).FirstOrDefault() ?? "User";
            Role = CurrentRole;
            IsSelf = await CurrentUserIdAsync() == Target.Id;
        }
        return Page();
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (IsNew)
        {
            if (string.IsNullOrWhiteSpace(NewPassword) || NewPassword.Length < 8)
                ModelState.AddModelError(nameof(NewPassword), "Passwort min. 8 Zeichen.");
            if (!ModelState.IsValid) return Page();

            var (res, _) = await userAdmin.CreateAsync(Email, DisplayName ?? "", NewPassword!, Role);
            if (!res.Succeeded)
            {
                foreach (var e in res.Errors) ModelState.AddModelError("", e.Description);
                return Page();
            }
            return RedirectToPage("/Users/Index");
        }

        Target = await userManager.FindByIdAsync(Id!);
        if (Target is null) return NotFound();
        IsSelf = await CurrentUserIdAsync() == Target.Id;
        CurrentRole = (await userManager.GetRolesAsync(Target)).FirstOrDefault() ?? "User";

        if (!ModelState.IsValid) return Page();

        // Profil-Update geht immer
        var upd = await userAdmin.UpdateProfileAsync(Target.Id, Email, DisplayName);
        if (!upd.Succeeded)
        {
            foreach (var e in upd.Errors) ModelState.AddModelError("", e.Description);
            return Page();
        }

        // Selbst-Schutz: eigene Rolle / Aktiv-Status NICHT ändern
        if (!IsSelf)
        {
            if (Role != CurrentRole) await userAdmin.SetRoleAsync(Target, Role);
            if (IsActive != Target.IsActive) await userAdmin.SetActiveAsync(Target.Id, IsActive);
        }

        // Optionaler Passwort-Reset
        if (!string.IsNullOrWhiteSpace(NewPassword))
        {
            if (NewPassword.Length < 8)
            {
                ModelState.AddModelError(nameof(NewPassword), "Passwort min. 8 Zeichen.");
                return Page();
            }
            var pwd = await userAdmin.ResetPasswordAsync(Target.Id, NewPassword);
            if (!pwd.Succeeded)
            {
                foreach (var e in pwd.Errors) ModelState.AddModelError("", e.Description);
                return Page();
            }
        }

        TempData["StatusMessage"] = "Änderungen gespeichert.";
        return RedirectToPage(new { Id = Target.Id });
    }

    public async Task<IActionResult> OnPostDeleteAsync()
    {
        if (IsNew) return NotFound();
        var meId = await CurrentUserIdAsync();
        if (meId == Id) return Forbid(); // Selbst-Schutz

        var res = await userAdmin.DeleteAsync(Id!);
        if (!res.Succeeded)
        {
            TempData["StatusError"] = string.Join(", ", res.Errors.Select(e => e.Description));
            return RedirectToPage(new { Id });
        }
        return RedirectToPage("/Users/Index");
    }
}
