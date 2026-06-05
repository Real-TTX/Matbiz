using Matbiz.Web.Data;
using Matbiz.Web.Modules.Customers.Models;
using Matbiz.Web.Shared;
using Microsoft.EntityFrameworkCore;

namespace Matbiz.Web.Modules.Customers.Services;

public class CustomerService(ApplicationDbContext db, ICurrentUserAccessor currentUser)
{
    public Task<List<Customer>> ListAsync(string? search = null, IReadOnlyCollection<Guid>? tagIds = null, CancellationToken ct = default)
    {
        var q = db.Customers.AsNoTracking()
            .Include(c => c.Tags).ThenInclude(t => t.Tag)
            .Include(c => c.Company)
            .AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(x => x.Name.Contains(s)
                || (x.CompanyName != null && x.CompanyName.Contains(s))
                || (x.Company != null && x.Company.Name.Contains(s))
                || (x.Email != null && x.Email.Contains(s)));
        }
        if (tagIds is { Count: > 0 })
        {
            q = q.Where(x => x.Tags.Any(t => tagIds.Contains(t.TagId)));
        }
        return q.OrderBy(x => x.Name).Take(500).ToListAsync(ct);
    }

    public Task<Customer?> GetAsync(Guid id, CancellationToken ct = default) =>
        db.Customers
            .Include(x => x.History.OrderByDescending(h => h.At))
            .Include(x => x.Tags).ThenInclude(t => t.Tag)
            .Include(x => x.Company)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

    /// <summary>Lookup table for tasks/dashboard rendering — id → display label.
    /// Returns only the requested ids so callers can render efficiently.</summary>
    public async Task<Dictionary<Guid, string>> NamesByIdAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        var distinct = ids.Distinct().ToList();
        if (distinct.Count == 0) return new();
        return await db.Customers.AsNoTracking()
            .Where(c => distinct.Contains(c.Id))
            .ToDictionaryAsync(
                c => c.Id,
                c => string.IsNullOrEmpty(c.CompanyName) ? c.Name : $"{c.Name} ({c.CompanyName})",
                ct);
    }

    /// <summary>Lightweight list for picker dropdowns — id + display name only.</summary>
    public Task<List<(Guid Id, string Label)>> ListLightAsync(CancellationToken ct = default) =>
        db.Customers.AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new ValueTuple<Guid, string>(c.Id, string.IsNullOrEmpty(c.CompanyName) ? c.Name : c.Name + " (" + c.CompanyName + ")"))
            .ToListAsync(ct);

    /// <summary>Most recently created contacts — for the dashboard "Neue Kontakte" widget.</summary>
    public Task<List<Customer>> RecentlyCreatedAsync(int take, CancellationToken ct = default) =>
        db.Customers.AsNoTracking()
            .Include(c => c.Company)
            .OrderByDescending(c => c.CreatedAt)
            .Take(take)
            .ToListAsync(ct);

    /// <summary>Latest history entries across ALL contacts — for the dashboard timeline widget.
    /// Includes the related customer so the UI can deep-link.</summary>
    public Task<List<CustomerHistoryEntry>> RecentHistoryAsync(int take, CancellationToken ct = default) =>
        db.CustomerHistoryEntries.AsNoTracking()
            .Include(h => h.Customer)
            .OrderByDescending(h => h.At)
            .Take(take)
            .ToListAsync(ct);

    public async Task<Customer> CreateAsync(Customer customer, CancellationToken ct = default)
    {
        customer.CreatedAt = customer.UpdatedAt = DateTime.UtcNow;
        db.Customers.Add(customer);
        await WriteHistoryAsync(customer.Id, "Created", $"Kunde angelegt: {customer.Name}", ct);
        await db.SaveChangesAsync(ct);
        return customer;
    }

    public async Task UpdateAsync(Customer customer, CancellationToken ct = default)
    {
        customer.UpdatedAt = DateTime.UtcNow;
        db.Customers.Update(customer);
        await WriteHistoryAsync(customer.Id, "Updated", $"Kunde aktualisiert: {customer.Name}", ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var c = await db.Customers.FindAsync([id], ct);
        if (c is null) return;
        db.Customers.Remove(c);
        await db.SaveChangesAsync(ct);
    }

    public async Task AddNoteAsync(Guid customerId, string note, CancellationToken ct = default)
    {
        await WriteHistoryAsync(customerId, "Note", note, ct);
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Details eines bestehenden Eintrags ändern. Wer/wann wird festgehalten.</summary>
    public async Task UpdateHistoryAsync(Guid entryId, string newDetails, CancellationToken ct = default)
    {
        var h = await db.CustomerHistoryEntries.FindAsync([entryId], ct);
        if (h is null) return;
        h.Details = newDetails;
        h.EditedAt = DateTime.UtcNow;
        var ctx = await currentUser.GetAsync();
        h.EditedByUserId = ctx.UserId;
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteHistoryAsync(Guid entryId, CancellationToken ct = default)
    {
        var h = await db.CustomerHistoryEntries.FindAsync([entryId], ct);
        if (h is null) return;
        db.CustomerHistoryEntries.Remove(h);
        await db.SaveChangesAsync(ct);
    }

    private async Task WriteHistoryAsync(Guid customerId, string action, string details, CancellationToken ct)
    {
        var ctx = await currentUser.GetAsync();
        db.CustomerHistoryEntries.Add(new CustomerHistoryEntry
        {
            CustomerId = customerId,
            Action = action,
            Details = details,
            ActorUserId = ctx.UserId ?? string.Empty,
            OnBehalfOfAdminId = ctx.ImpersonatorId
        });
    }
}

// CustomerFieldService entfernt — Custom-Fields werden nun über
// Modules/CustomFields/Services/CustomFieldService verwaltet.
