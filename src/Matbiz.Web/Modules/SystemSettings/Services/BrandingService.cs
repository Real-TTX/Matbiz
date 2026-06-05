using Matbiz.Web.Data;
using Matbiz.Web.Modules.SystemSettings.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Matbiz.Web.Modules.SystemSettings.Services;

public class BrandingService(ApplicationDbContext db, IMemoryCache cache, ILogger<BrandingService> logger)
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

    public async Task UpdateMetaAsync(string appName, string primaryColor, string accent1, string accent2,
        int logoHeightPx = 40, bool showAppNameUnderLogo = true, CancellationToken ct = default)
    {
        var row = await db.BrandingSettings.FirstOrDefaultAsync(ct);
        if (row is null)
        {
            row = new BrandingSettings
            {
                AppName = appName,
                PrimaryColor = primaryColor,
                AccentColor1 = accent1,
                AccentColor2 = accent2,
                LogoHeightPx = Math.Clamp(logoHeightPx, 24, 96),
                ShowAppNameUnderLogo = showAppNameUnderLogo
            };
            db.BrandingSettings.Add(row);
        }
        else
        {
            row.AppName = appName;
            row.PrimaryColor = primaryColor;
            row.AccentColor1 = accent1;
            row.AccentColor2 = accent2;
            row.LogoHeightPx = Math.Clamp(logoHeightPx, 24, 96);
            row.ShowAppNameUnderLogo = showAppNameUnderLogo;
        }
        row.UpdatedAt = DateTime.UtcNow;
        row.Version++;
        await db.SaveChangesAsync(ct);
        cache.Remove(CacheKey);
    }

    /// <summary>Updatet Firma-Stammdaten (Adresse, USt-ID, Bank, PDF-Texte).</summary>
    public async Task UpdateCompanyAsync(BrandingSettings input, CancellationToken ct = default)
    {
        var row = await db.BrandingSettings.FirstOrDefaultAsync(ct) ?? new BrandingSettings();

        row.CompanyLegalName = input.CompanyLegalName;
        row.CompanyStreet = input.CompanyStreet;
        row.CompanyPostalCode = input.CompanyPostalCode;
        row.CompanyCity = input.CompanyCity;
        row.CompanyCountry = input.CompanyCountry;
        row.CompanyEmail = input.CompanyEmail;
        row.CompanyPhone = input.CompanyPhone;
        row.CompanyWebsite = input.CompanyWebsite;
        row.VatId = input.VatId;
        row.TaxNumber = input.TaxNumber;
        row.ManagingDirector = input.ManagingDirector;
        row.RegistrationCourt = input.RegistrationCourt;
        row.RegistrationNumber = input.RegistrationNumber;
        row.BankName = input.BankName;
        row.Iban = input.Iban;
        row.Bic = input.Bic;
        row.DefaultPaymentTerms = input.DefaultPaymentTerms;
        row.PdfFooterText = input.PdfFooterText;
        if (!string.IsNullOrWhiteSpace(input.PdfTemplate)) row.PdfTemplate = input.PdfTemplate;
        if (!string.IsNullOrWhiteSpace(input.ChartOfAccounts)) row.ChartOfAccounts = input.ChartOfAccounts;

        row.UpdatedAt = DateTime.UtcNow;
        row.Version++;
        if (row.Id == default) db.BrandingSettings.Add(row);
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
        await db.SaveChangesAsync(ct);
        cache.Remove(CacheKey);
        logger.LogInformation("Light logo set ({Size} bytes, {Type}).", bytes.Length, contentType);
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

    public async Task SetDarkLogoAsync(byte[] bytes, string contentType, CancellationToken ct = default)
    {
        var row = await db.BrandingSettings.FirstOrDefaultAsync(ct) ?? new BrandingSettings();
        row.LogoDarkBytes = bytes;
        row.LogoDarkContentType = contentType;
        row.UpdatedAt = DateTime.UtcNow;
        row.Version++;
        if (row.Id == default) db.BrandingSettings.Add(row);
        await db.SaveChangesAsync(ct);
        cache.Remove(CacheKey);
        logger.LogInformation("Dark logo set ({Size} bytes, {Type}).", bytes.Length, contentType);
    }

    public async Task ClearDarkLogoAsync(CancellationToken ct = default)
    {
        var row = await db.BrandingSettings.FirstOrDefaultAsync(ct);
        if (row is null) return;
        row.LogoDarkBytes = null;
        row.LogoDarkContentType = null;
        row.UpdatedAt = DateTime.UtcNow;
        row.Version++;
        await db.SaveChangesAsync(ct);
        cache.Remove(CacheKey);
    }
}
