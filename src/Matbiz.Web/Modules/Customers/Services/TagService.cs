using Matbiz.Web.Data;
using Matbiz.Web.Modules.Customers.Models;
using Microsoft.EntityFrameworkCore;

namespace Matbiz.Web.Modules.Customers.Services;

public class TagService(ApplicationDbContext db)
{
    public Task<List<Tag>> ListAsync(CancellationToken ct = default) =>
        db.Tags.AsNoTracking().OrderBy(t => t.Name).ToListAsync(ct);

    public async Task<Tag> EnsureAsync(string name, string? color = null, CancellationToken ct = default)
    {
        var normalized = name.Trim();
        var existing = await db.Tags.FirstOrDefaultAsync(t => t.Name.ToLower() == normalized.ToLower(), ct);
        if (existing is not null) return existing;

        var tag = new Tag { Name = normalized, Color = color ?? RandomColor(normalized) };
        db.Tags.Add(tag);
        await db.SaveChangesAsync(ct);
        return tag;
    }

    public async Task AddToCustomerAsync(Guid customerId, string tagName, CancellationToken ct = default)
    {
        var tag = await EnsureAsync(tagName, ct: ct);
        var link = await db.CustomerTags.FirstOrDefaultAsync(x => x.CustomerId == customerId && x.TagId == tag.Id, ct);
        if (link is not null) return;
        db.CustomerTags.Add(new CustomerTag { CustomerId = customerId, TagId = tag.Id });
        await db.SaveChangesAsync(ct);
    }

    public async Task RemoveFromCustomerAsync(Guid customerId, Guid tagId, CancellationToken ct = default)
    {
        var link = await db.CustomerTags.FirstOrDefaultAsync(x => x.CustomerId == customerId && x.TagId == tagId, ct);
        if (link is null) return;
        db.CustomerTags.Remove(link);
        await db.SaveChangesAsync(ct);
    }

    public Task<List<Tag>> ForCustomerAsync(Guid customerId, CancellationToken ct = default) =>
        db.CustomerTags.AsNoTracking()
            .Where(ct1 => ct1.CustomerId == customerId)
            .Select(ct1 => ct1.Tag)
            .OrderBy(t => t.Name)
            .ToListAsync(ct);

    // --- Companies: same shared Tag pool, separate join table ----------------
    public async Task AddToCompanyAsync(Guid companyId, string tagName, CancellationToken ct = default)
    {
        var tag = await EnsureAsync(tagName, ct: ct);
        var link = await db.CompanyTags.FirstOrDefaultAsync(x => x.CompanyId == companyId && x.TagId == tag.Id, ct);
        if (link is not null) return;
        db.CompanyTags.Add(new CompanyTag { CompanyId = companyId, TagId = tag.Id });
        await db.SaveChangesAsync(ct);
    }

    public async Task RemoveFromCompanyAsync(Guid companyId, Guid tagId, CancellationToken ct = default)
    {
        var link = await db.CompanyTags.FirstOrDefaultAsync(x => x.CompanyId == companyId && x.TagId == tagId, ct);
        if (link is null) return;
        db.CompanyTags.Remove(link);
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Deterministic-ish color per tag name so chips look consistent before the user customizes.</summary>
    private static string RandomColor(string name)
    {
        string[] palette = { "#1849a9", "#9a3412", "#027a48", "#b54708", "#b42318", "#5b21b6", "#0e7490", "#7e22ce" };
        var sum = name.Sum(c => c);
        return palette[Math.Abs(sum) % palette.Length];
    }
}
