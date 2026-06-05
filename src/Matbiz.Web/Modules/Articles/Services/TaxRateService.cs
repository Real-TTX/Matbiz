using Matbiz.Web.Data;
using Matbiz.Web.Modules.Articles.Models;
using Microsoft.EntityFrameworkCore;

namespace Matbiz.Web.Modules.Articles.Services;

public class TaxRateService(ApplicationDbContext db)
{
    /// <summary>DE-Default-Set — 19% / 7% / 0%.</summary>
    public static readonly TaxRate[] Defaults =
    {
        new() { Name = "Voll 19 %",   Percent = 19m, IsDefault = true,  SortOrder = 1 },
        new() { Name = "Ermäßigt 7 %", Percent = 7m,  IsDefault = false, SortOrder = 2 },
        new() { Name = "Keine 0 %",   Percent = 0m,  IsDefault = false, SortOrder = 3 },
    };

    public Task<List<TaxRate>> ListAsync(CancellationToken ct = default) =>
        db.TaxRates.AsNoTracking().OrderBy(x => x.SortOrder).ThenBy(x => x.Name).ToListAsync(ct);

    /// <summary>Seedet DE-Standard-Steuersätze wenn Tabelle leer (Startup).</summary>
    public async Task EnsureDefaultsAsync(CancellationToken ct = default)
    {
        if (await db.TaxRates.AnyAsync(ct)) return;
        foreach (var d in Defaults)
        {
            db.TaxRates.Add(new TaxRate
            {
                Name = d.Name, Percent = d.Percent,
                IsDefault = d.IsDefault, SortOrder = d.SortOrder
            });
        }
        await db.SaveChangesAsync(ct);
    }

    public Task<TaxRate?> GetAsync(Guid id, CancellationToken ct = default) =>
        db.TaxRates.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<TaxRate?> GetDefaultAsync(CancellationToken ct = default)
    {
        var def = await db.TaxRates.AsNoTracking()
            .FirstOrDefaultAsync(x => x.IsDefault, ct);
        return def ?? await db.TaxRates.AsNoTracking().OrderBy(x => x.SortOrder).FirstOrDefaultAsync(ct);
    }

    public async Task<TaxRate> CreateAsync(TaxRate tr, CancellationToken ct = default)
    {
        if (tr.IsDefault) await ClearDefaultsAsync(ct);
        db.TaxRates.Add(tr);
        await db.SaveChangesAsync(ct);
        return tr;
    }

    public async Task UpdateAsync(TaxRate tr, CancellationToken ct = default)
    {
        if (tr.IsDefault) await ClearDefaultsAsync(ct, except: tr.Id);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var inUse = await db.Articles.AnyAsync(a => a.TaxRateId == id, ct);
        if (inUse) throw new InvalidOperationException("Steuersatz wird von Artikeln verwendet und kann nicht gelöscht werden.");
        var tr = await db.TaxRates.FindAsync([id], ct);
        if (tr is null) return;
        db.TaxRates.Remove(tr);
        await db.SaveChangesAsync(ct);
    }

    private async Task ClearDefaultsAsync(CancellationToken ct, Guid? except = null)
    {
        var all = await db.TaxRates.Where(x => x.IsDefault && x.Id != except).ToListAsync(ct);
        foreach (var t in all) t.IsDefault = false;
    }
}
