using Matbiz.Web.Data;
using Matbiz.Web.Modules.Customers.Models;
using Matbiz.Web.Shared;
using Microsoft.EntityFrameworkCore;

namespace Matbiz.Web.Modules.Customers.Services;

public class CompanyService(ApplicationDbContext db, ICurrentUserAccessor currentUser)
{
    public Task<List<Company>> ListAsync(string? search = null, IReadOnlyCollection<Guid>? tagIds = null, CancellationToken ct = default)
    {
        var q = db.Companies.AsNoTracking()
            .Include(c => c.Tags).ThenInclude(t => t.Tag)
            .AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(c => c.Name.Contains(s) || (c.City != null && c.City.Contains(s)) || (c.Email != null && c.Email.Contains(s)));
        }
        if (tagIds is { Count: > 0 })
            q = q.Where(c => c.Tags.Any(t => tagIds.Contains(t.TagId)));
        return q.OrderBy(c => c.Name).Take(500).ToListAsync(ct);
    }

    public Task<Company?> GetAsync(Guid id, CancellationToken ct = default) =>
        db.Companies
            .Include(c => c.Contacts)
            .Include(c => c.Tags).ThenInclude(t => t.Tag)
            .Include(c => c.History.OrderByDescending(h => h.At))
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task AddNoteAsync(Guid companyId, string note, CancellationToken ct = default)
    {
        var ctx = await currentUser.GetAsync();
        db.CompanyHistoryEntries.Add(new CompanyHistoryEntry
        {
            CompanyId = companyId,
            Action = "Note",
            Details = note,
            ActorUserId = ctx.UserId ?? string.Empty,
            OnBehalfOfAdminId = ctx.ImpersonatorId
        });
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateHistoryAsync(Guid entryId, string newDetails, CancellationToken ct = default)
    {
        var h = await db.CompanyHistoryEntries.FindAsync([entryId], ct);
        if (h is null) return;
        h.Details = newDetails;
        h.EditedAt = DateTime.UtcNow;
        var ctx = await currentUser.GetAsync();
        h.EditedByUserId = ctx.UserId;
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteHistoryAsync(Guid entryId, CancellationToken ct = default)
    {
        var h = await db.CompanyHistoryEntries.FindAsync([entryId], ct);
        if (h is null) return;
        db.CompanyHistoryEntries.Remove(h);
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Returns history entries for the company, optionally merged with
    /// entries from all its linked contacts. Each merged entry carries a label
    /// pointing at the source contact so the UI can show provenance.</summary>
    public async Task<List<CompanyHistoryView>> GetHistoryAsync(Guid companyId, bool includeContacts, CancellationToken ct = default)
    {
        var own = await db.CompanyHistoryEntries.AsNoTracking()
            .Where(h => h.CompanyId == companyId)
            .Select(h => new CompanyHistoryView(
                h.Id, h.At, h.Action, h.Details, h.ActorUserId, h.OnBehalfOfAdminId,
                "Firma", (Guid?)null))
            .ToListAsync(ct);

        if (!includeContacts) return own.OrderByDescending(x => x.At).ToList();

        var contactRows = await db.CustomerHistoryEntries.AsNoTracking()
            .Where(h => db.Customers.Any(c => c.Id == h.CustomerId && c.CompanyId == companyId))
            .Join(db.Customers.AsNoTracking(),
                  h => h.CustomerId,
                  c => c.Id,
                  (h, c) => new CompanyHistoryView(
                      h.Id, h.At, h.Action, h.Details, h.ActorUserId, h.OnBehalfOfAdminId,
                      "Kontakt: " + c.Name, c.Id))
            .ToListAsync(ct);

        return own.Concat(contactRows).OrderByDescending(x => x.At).ToList();
    }

    public async Task<Company> CreateAsync(Company company, CancellationToken ct = default)
    {
        db.Companies.Add(company);
        await db.SaveChangesAsync(ct);
        return company;
    }

    public async Task UpdateAsync(Company company, CancellationToken ct = default)
    {
        db.Companies.Update(company);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var c = await db.Companies.FindAsync([id], ct);
        if (c is null) return;
        db.Companies.Remove(c);
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Most recently created companies — for the dashboard "Neue Firmen" widget.</summary>
    public Task<List<Company>> RecentlyCreatedAsync(int take, CancellationToken ct = default) =>
        db.Companies.AsNoTracking()
            .OrderByDescending(c => c.CreatedAt)
            .Take(take)
            .ToListAsync(ct);
}
