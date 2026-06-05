using Matbiz.Web.Data;
using Matbiz.Web.Modules.Warehouse.Models;
using Microsoft.EntityFrameworkCore;

namespace Matbiz.Web.Modules.Warehouse.Services;

public class WarehouseService(ApplicationDbContext db)
{
    public Task<List<Models.Warehouse>> ListAsync(bool includeInactive = false, CancellationToken ct = default)
    {
        IQueryable<Models.Warehouse> q = db.Warehouses.AsNoTracking();
        if (!includeInactive) q = q.Where(w => w.IsActive);
        return q.OrderByDescending(w => w.IsDefault).ThenBy(w => w.Name).ToListAsync(ct);
    }

    public Task<Models.Warehouse?> GetAsync(Guid id, CancellationToken ct = default) =>
        db.Warehouses.FirstOrDefaultAsync(w => w.Id == id, ct);

    public Task<Models.Warehouse?> GetDefaultAsync(CancellationToken ct = default) =>
        db.Warehouses.AsNoTracking()
            .Where(w => w.IsActive)
            .OrderByDescending(w => w.IsDefault).ThenBy(w => w.CreatedAt)
            .FirstOrDefaultAsync(ct);

    public async Task<Models.Warehouse> CreateAsync(string name, bool isDefault = false, CancellationToken ct = default)
    {
        if (isDefault)
        {
            var current = await db.Warehouses.Where(w => w.IsDefault).ToListAsync(ct);
            foreach (var c in current) c.IsDefault = false;
        }
        var w = new Models.Warehouse { Name = name.Trim(), IsDefault = isDefault };
        db.Warehouses.Add(w);
        await db.SaveChangesAsync(ct);
        return w;
    }

    public async Task UpdateAsync(Models.Warehouse w, CancellationToken ct = default)
    {
        if (w.IsDefault)
        {
            var others = await db.Warehouses.Where(x => x.IsDefault && x.Id != w.Id).ToListAsync(ct);
            foreach (var o in others) o.IsDefault = false;
        }
        await db.SaveChangesAsync(ct);
    }

    public async Task EnsureDefaultAsync(CancellationToken ct = default)
    {
        if (await db.Warehouses.AnyAsync(ct)) return;
        db.Warehouses.Add(new Models.Warehouse { Name = "Hauptlager", IsDefault = true });
        await db.SaveChangesAsync(ct);
    }
}
