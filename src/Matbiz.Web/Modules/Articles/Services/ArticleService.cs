using Matbiz.Web.Data;
using Matbiz.Web.Modules.Articles.Models;
using Microsoft.EntityFrameworkCore;

namespace Matbiz.Web.Modules.Articles.Services;

public class ArticleService(
    ApplicationDbContext db,
    NumberRangeService numberRanges,
    TaxRateService taxRates)
{
    public Task<List<Article>> ListAsync(bool includeInactive = true, CancellationToken ct = default)
    {
        IQueryable<Article> q = db.Articles.AsNoTracking().Include(a => a.TaxRate);
        if (!includeInactive) q = q.Where(a => a.IsActive);
        return q.OrderBy(a => a.SortOrder).ThenBy(a => a.Name).ToListAsync(ct);
    }

    public Task<Article?> GetAsync(Guid id, CancellationToken ct = default) =>
        db.Articles.Include(a => a.TaxRate).FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<Article> CreateAsync(Article article, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(article.Number))
            article.Number = await numberRanges.NextAsync("Article", ct);

        if (article.TaxRateId == Guid.Empty)
        {
            var def = await taxRates.GetDefaultAsync(ct);
            if (def is not null) article.TaxRateId = def.Id;
        }

        article.CreatedAt = article.UpdatedAt = DateTime.UtcNow;
        db.Articles.Add(article);
        await db.SaveChangesAsync(ct);
        return article;
    }

    public async Task UpdateAsync(Article article, CancellationToken ct = default)
    {
        article.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var a = await db.Articles.FindAsync([id], ct);
        if (a is null) return;
        db.Articles.Remove(a);
        await db.SaveChangesAsync(ct);
    }
}
