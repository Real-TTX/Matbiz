using Matbiz.Web.Data;
using Matbiz.Web.Modules.Teams.Models;
using Microsoft.EntityFrameworkCore;

namespace Matbiz.Web.Modules.Teams.Services;

public class DepartmentService(ApplicationDbContext db)
{
    public Task<List<Department>> ListAsync(CancellationToken ct = default) =>
        db.Departments.AsNoTracking()
            .Include(d => d.Teams)
            .OrderBy(d => d.Name)
            .ToListAsync(ct);

    public Task<Department?> GetAsync(Guid id, CancellationToken ct = default) =>
        db.Departments.Include(d => d.Teams).FirstOrDefaultAsync(d => d.Id == id, ct);

    public async Task<Department> CreateAsync(Department dept, CancellationToken ct = default)
    {
        db.Departments.Add(dept);
        await db.SaveChangesAsync(ct);
        return dept;
    }

    public async Task UpdateAsync(Department dept, CancellationToken ct = default)
    {
        db.Departments.Update(dept);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var d = await db.Departments.FindAsync([id], ct);
        if (d is null) return;
        db.Departments.Remove(d);
        await db.SaveChangesAsync(ct);
    }
}
