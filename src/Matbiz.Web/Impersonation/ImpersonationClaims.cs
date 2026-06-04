using System.Security.Claims;
using Matbiz.Web.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;

namespace Matbiz.Web.Impersonation;

/// <summary>
/// Claim type names used to mark an impersonated principal. The presence of
/// <see cref="ImpersonatorId"/> means: "this principal is target user X but the
/// real human acting is admin Y" — used for the banner and for audit trails.
/// </summary>
public static class ImpersonationClaims
{
    public const string ImpersonatorId = "matbiz:impersonator_id";
    public const string ImpersonatorName = "matbiz:impersonator_name";
}

/// <summary>
/// Runs on every authenticated request. If the signed-in admin has an active
/// impersonation session in the DB, the principal is rebuilt as the target
/// user with two extra claims marking the real admin. This is the
/// server-side enforcement point.
/// </summary>
public class ImpersonationClaimsTransformation(
    IServiceScopeFactory scopes) : IClaimsTransformation
{
    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true)
            return principal;

        // Already transformed earlier in the pipeline — avoid double-wrapping.
        if (principal.HasClaim(c => c.Type == ImpersonationClaims.ImpersonatorId))
            return principal;

        var adminUserId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(adminUserId))
            return principal;

        using var scope = scopes.CreateScope();
        var imp = scope.ServiceProvider.GetRequiredService<IImpersonationService>();
        var session = await imp.GetActiveForAdminAsync(adminUserId);
        if (session is null) return principal;

        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var target = await users.FindByIdAsync(session.TargetUserId);
        if (target is null || !target.IsActive) return principal;

        var signInFactory = scope.ServiceProvider.GetRequiredService<IUserClaimsPrincipalFactory<ApplicationUser>>();
        var targetPrincipal = await signInFactory.CreateAsync(target);
        var identity = (ClaimsIdentity)targetPrincipal.Identity!;

        var adminName = principal.FindFirstValue(ClaimTypes.Name) ?? adminUserId;
        identity.AddClaim(new Claim(ImpersonationClaims.ImpersonatorId, adminUserId));
        identity.AddClaim(new Claim(ImpersonationClaims.ImpersonatorName, adminName));

        return targetPrincipal;
    }
}
