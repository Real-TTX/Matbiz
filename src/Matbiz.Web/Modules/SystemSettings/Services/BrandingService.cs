using Matbiz.Web.Data;
using Matbiz.Web.Modules.SystemSettings.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Matbiz.Web.Modules.SystemSettings.Services;

public class BrandingService(ApplicationDbContext db, IMemoryCache cache)
{
    private const string CacheKey = "branding:current";

    public async Task<BrandingSettings> GetAsync(CancellationToken ct = default)
    {
        if (cache.TryGetValue(CacheKey, out BrandingSettings? cached) && cached is not null)
            return cached;

        var row = await db.BrandingSettings.AsNoTracking().FirstOrDefaultAsync(ct)
                  ?? new BrandingSettings();
        cache.Set(CacheKey, row, TimeSpan.FromMinutes(10));
        return row;
    }

    public async Task UpdateMetaAsync(string appName, string primaryColor, string accent1, string accent2, CancellationToken ct = default)
    {
        var row = await db.BrandingSettings.FirstOrDefaultAsync(ct);
        if (row is null)
        {
            row = new BrandingSettings
            {
                AppName = appName,
                PrimaryColor = primaryColor,
                AccentColor1 = accent1,
                AccentColor2 = accent2
            };
            db.BrandingSettings.Add(row);
        }
        else
        {
            row.AppName = appName;
            row.PrimaryColor = primaryColor;
            row.AccentColor1 = accent1;
            row.AccentColor2 = accent2;
        }
        row.UpdatedAt = DateTime.UtcNow;
        row.Version++;
        await db.SaveChangesAsync(ct);
        cache.Remove(CacheKey);
    }

    public async Task SetLogoAsync(byte[] bytes, string contentType, CancellationToken ct = default)
    {
        var row = await db.BrandingSettings.FirstOrDefaultAsync(ct) ?? new BrandingSettings();
        row.LogoBytes = bytes;
        row.LogoContentType = contentType;
        row.UpdatedAt = DateTime.UtcNow;
        row.Version++;
        if (row.Id == default) db.BrandingSettings.Add(row);
        else db.BrandingSettings.Update(row);
        await db.SaveChangesAsync(ct);
        cache.Remove(CacheKey);
    }

    public async Task ClearLogoAsync(CancellationToken ct = default)
    {
        var row = await db.BrandingSettings.FirstOrDefaultAsync(ct);
        if (row is null) return;
        row.LogoBytes = null;
        row.LogoContentType = null;
        row.UpdatedAt = DateTime.UtcNow;
        row.Version++;
        await db.SaveChangesAsync(ct);
        cache.Remove(CacheKey);
    }
}
