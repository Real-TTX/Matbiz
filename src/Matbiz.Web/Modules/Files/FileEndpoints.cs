using Matbiz.Web.Modules.Files.Services;
using Microsoft.AspNetCore.Authorization;

namespace Matbiz.Web.Modules.Files;

public static class FileEndpoints
{
    /// <summary>
    /// Single download endpoint for any attached file. Authentication required;
    /// in this internal ERP we trust any authenticated user with knowledge of
    /// the file id (the id is not guessable). Tighten by owner-type checks
    /// later if multi-tenant isolation becomes a requirement.
    /// </summary>
    public static IEndpointRouteBuilder MapFileEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/files/{id:guid}", async (Guid id, AttachedFileService files, HttpContext http) =>
        {
            var f = await files.GetAsync(id, http.RequestAborted);
            if (f is null) return Results.NotFound();

            http.Response.Headers.CacheControl = "private, max-age=300";
            return Results.File(f.Content, f.ContentType, f.FileName);
        }).RequireAuthorization();

        return endpoints;
    }
}
