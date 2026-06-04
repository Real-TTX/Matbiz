using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Matbiz.Web.Impersonation;

public static class ImpersonationEndpoints
{
    public static IEndpointRouteBuilder MapImpersonationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // Start: only an admin (not currently impersonating) may initiate.
        endpoints.MapPost("/impersonation/start", async (
            HttpContext http,
            [FromForm] string targetUserId,
            [FromForm] string? reason,
            [FromForm] string? returnUrl,
            IImpersonationService impersonation) =>
        {
            if (http.User.Identity?.IsAuthenticated != true) return Results.Unauthorized();

            // Inside an impersonated session there is no admin role on the principal,
            // so we identify the real admin via the impersonator claim (if any),
            // otherwise the current user must hold the Admin role.
            var adminId = http.User.ImpersonatorId() ?? http.User.UserId();
            if (string.IsNullOrEmpty(adminId)) return Results.Unauthorized();

            if (http.User.ImpersonatorId() is null && !http.User.IsInRole(Data.Roles.Admin))
                return Results.Forbid();

            await impersonation.StartAsync(adminId, targetUserId, reason);
            return Results.Redirect(SafeReturn(returnUrl, "/"));
        })
        .RequireAuthorization()
        .DisableAntiforgery();

        // End: any authenticated principal whose session is impersonated may end it.
        // The principal is the target user; the real admin id lives in the claim.
        endpoints.MapPost("/impersonation/end", async (
            HttpContext http,
            [FromForm] string? returnUrl,
            IImpersonationService impersonation) =>
        {
            if (http.User.Identity?.IsAuthenticated != true) return Results.Unauthorized();

            var adminId = http.User.ImpersonatorId();
            if (string.IsNullOrEmpty(adminId)) return Results.Redirect(SafeReturn(returnUrl, "/"));

            await impersonation.EndAsync(adminId);
            return Results.Redirect(SafeReturn(returnUrl, "/users"));
        })
        .RequireAuthorization()
        .DisableAntiforgery();

        return endpoints;
    }

    private static string SafeReturn(string? returnUrl, string fallback) =>
        !string.IsNullOrEmpty(returnUrl) && Uri.IsWellFormedUriString(returnUrl, UriKind.Relative)
            ? returnUrl
            : fallback;
}
