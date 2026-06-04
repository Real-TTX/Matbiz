using System.Security.Claims;

namespace Matbiz.Web.Impersonation;

/// <summary>
/// Convenience extension methods to read who is *effectively* acting and
/// who, if anyone, is impersonating them. Use these instead of poking
/// claim types directly in the UI / services.
/// </summary>
public static class CurrentUser
{
    public static string? UserId(this ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.NameIdentifier);

    public static string? UserName(this ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.Name);

    public static bool IsImpersonated(this ClaimsPrincipal user) =>
        user.HasClaim(c => c.Type == ImpersonationClaims.ImpersonatorId);

    public static string? ImpersonatorId(this ClaimsPrincipal user) =>
        user.FindFirstValue(ImpersonationClaims.ImpersonatorId);

    public static string? ImpersonatorName(this ClaimsPrincipal user) =>
        user.FindFirstValue(ImpersonationClaims.ImpersonatorName);
}
