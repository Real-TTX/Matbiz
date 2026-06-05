using Matbiz.Web.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Matbiz.Web.Modules.Articles;

public static class ArticleImageEndpoints
{
    public static IEndpointRouteBuilder MapArticleImage(this IEndpointRouteBuilder app)
    {
        app.MapGet("/articles/{id:guid}/image", [Authorize] async (Guid id, ApplicationDbContext db) =>
        {
            var img = await db.Articles
                .Where(a => a.Id == id)
                .Select(a => new { a.ImageBytes, a.ImageContentType, a.ImageVersion })
                .FirstOrDefaultAsync();
            if (img?.ImageBytes is null || img.ImageBytes.Length == 0)
                return Results.NotFound();
            return Results.File(img.ImageBytes, img.ImageContentType ?? "application/octet-stream");
        });
        return app;
    }
}
