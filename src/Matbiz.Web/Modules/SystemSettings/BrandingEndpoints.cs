using Matbiz.Web.Modules.SystemSettings.Services;

namespace Matbiz.Web.Modules.SystemSettings;

public static class BrandingEndpoints
{
    /// <summary>
    /// Public endpoint serving the current logo so the login page (anonymous)
    /// can render it. URL carries a version query param for cache-busting.
    /// </summary>
    public static IEndpointRouteBuilder MapBrandingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/branding/logo", async (BrandingService branding, HttpContext http) =>
        {
            var b = await branding.GetAsync(http.RequestAborted);
            if (b.LogoBytes is null || b.LogoBytes.Length == 0)
                return Results.NotFound();

            http.Response.Headers.CacheControl = "public, max-age=3600";
            return Results.File(b.LogoBytes, b.LogoContentType ?? "application/octet-stream");
        }).AllowAnonymous();

        return endpoints;
    }
}
