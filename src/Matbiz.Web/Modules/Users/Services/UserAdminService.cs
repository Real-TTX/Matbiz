using Matbiz.Web.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Matbiz.Web.Modules.Users.Services;

public class UserAdminService(
    UserManager<ApplicationUser> users,
    RoleManager<IdentityRole> roles)
{
    public Task<List<ApplicationUser>> ListAsync(CancellationToken ct = default) =>
        users.Users.OrderBy(u => u.UserName).ToListAsync(ct);

    public async Task<(IdentityResult result, ApplicationUser? user)> CreateAsync(string email, string displayName, string password, string role, CancellationToken ct = default)
    {
        var u = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            DisplayName = displayName,
            IsActive = true
        };
        var res = await users.CreateAsync(u, password);
        if (!res.Succeeded) return (res, null);

        if (!await roles.RoleExistsAsync(role))
            await roles.CreateAsync(new IdentityRole(role));
        await users.AddToRoleAsync(u, role);
        return (res, u);
    }

    public async Task<IdentityResult> SetActiveAsync(string userId, bool active)
    {
        var u = await users.FindByIdAsync(userId);
        if (u is null) return IdentityResult.Failed(new IdentityError { Description = "User nicht gefunden." });
        u.IsActive = active;
        return await users.UpdateAsync(u);
    }

    public async Task<IList<string>> GetRolesAsync(ApplicationUser u) => await users.GetRolesAsync(u);

    public async Task SetRoleAsync(ApplicationUser u, string role)
    {
        var current = await users.GetRolesAsync(u);
        await users.RemoveFromRolesAsync(u, current);
        if (!await roles.RoleExistsAsync(role))
            await roles.CreateAsync(new IdentityRole(role));
        await users.AddToRoleAsync(u, role);
    }

    public async Task<IdentityResult> DeleteAsync(string userId)
    {
        var u = await users.FindByIdAsync(userId);
        if (u is null) return IdentityResult.Failed(new IdentityError { Description = "User nicht gefunden." });
        return await users.DeleteAsync(u);
    }

    public async Task<IdentityResult> ResetPasswordAsync(string userId, string newPassword)
    {
        var u = await users.FindByIdAsync(userId);
        if (u is null) return IdentityResult.Failed(new IdentityError { Description = "User nicht gefunden." });
        var token = await users.GeneratePasswordResetTokenAsync(u);
        return await users.ResetPasswordAsync(u, token, newPassword);
    }

    public async Task<IdentityResult> UpdateProfileAsync(string userId, string email, string? displayName)
    {
        var u = await users.FindByIdAsync(userId);
        if (u is null) return IdentityResult.Failed(new IdentityError { Description = "User nicht gefunden." });
        u.Email = email;
        u.UserName = email;
        u.NormalizedEmail = email.ToUpperInvariant();
        u.NormalizedUserName = email.ToUpperInvariant();
        u.DisplayName = displayName ?? "";
        return await users.UpdateAsync(u);
    }
}
