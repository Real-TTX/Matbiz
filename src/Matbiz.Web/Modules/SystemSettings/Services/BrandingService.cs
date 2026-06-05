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
        int logoHeightPx = 40, bool showAppNameUnderLogo = true,
        string logoInvertMode = "None", CancellationToken ct = default)
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
                ShowAppNameUnderLogo = showAppNameUnderLogo,
                LogoInvertMode = NormalizeMode(logoInvertMode)
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
            row.LogoInvertMode = NormalizeMode(logoInvertMode);
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

    public async Task SetLogoAsync(byte[] bytes, string contentType,
        bool removeWhiteBackground = false, int whiteTolerancePercent = 85,
        string? recolorHex = null, CancellationToken ct = default)
    {
        // Wenn gewünscht: serverseitig prozessieren → immer PNG mit Transparenz
        if (removeWhiteBackground || !string.IsNullOrWhiteSpace(recolorHex))
        {
            try
            {
                // Mapping: alter 0..100-Luma-Slider → 0..200 Farbdistanz-Toleranz
                var tol = MapLumaPctToColorDistance(whiteTolerancePercent);
                var r = LogoProcessor.Process(bytes, new LogoProcessor.Options(
                    RemoveBackground: removeWhiteBackground,
                    ColorTolerance: tol,
                    RecolorHex: recolorHex));
                bytes = r.Bytes;
                contentType = "image/png";
                logger.LogInformation("Logo processed: BG detected = {Bg}, transparent pixels = {Tp}/{Total}",
                    r.DetectedBg, r.PixelsMadeTransparent, r.TotalPixels);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Logo processing failed — keeping original bytes.");
            }
        }

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

    private static string NormalizeMode(string? m) => m switch
    {
        "DarkOnly" => "DarkOnly",
        "Always"   => "Always",
        _          => "None"
    };

    /// <summary>
    /// Nimmt das aktuell gespeicherte Logo und wendet die Transparenz-/
    /// Einfärbungs-Verarbeitung erneut darauf an (ohne Neu-Upload).
    /// </summary>
    public async Task ReprocessLogoAsync(bool removeWhiteBackground, int whiteTolerancePercent,
        string? recolorHex, CancellationToken ct = default)
    {
        var row = await db.BrandingSettings.FirstOrDefaultAsync(ct);
        if (row?.LogoBytes is not { Length: > 0 } original)
            throw new InvalidOperationException("Kein Logo gespeichert.");

        // SVG vorab abfangen — ImageSharp 2.x kann das nicht
        if (LooksLikeSvg(original) || (row.LogoContentType?.Contains("svg") ?? false))
            throw new NotSupportedException(
                "SVG kann nicht serverseitig bearbeitet werden. " +
                "Bitte das Logo als PNG oder JPG hochladen, dann klappt die Hintergrund-Entfernung.");

        var tol = MapLumaPctToColorDistance(whiteTolerancePercent);
        var r = LogoProcessor.Process(original, new LogoProcessor.Options(
            RemoveBackground: removeWhiteBackground,
            ColorTolerance: tol,
            RecolorHex: recolorHex));
        row.LogoBytes = r.Bytes;
        row.LogoContentType = "image/png";
        row.UpdatedAt = DateTime.UtcNow;
        row.Version++;
        await db.SaveChangesAsync(ct);
        cache.Remove(CacheKey);
        logger.LogInformation("Logo reprocessed: BG = {Bg}, transparent = {Tp}/{Total}",
            r.DetectedBg, r.PixelsMadeTransparent, r.TotalPixels);
    }

    private static bool LooksLikeSvg(byte[] bytes)
    {
        if (bytes.Length < 5) return false;
        // SVG-Dateien beginnen typisch mit "<?xml" oder direkt "<svg"
        var head = System.Text.Encoding.UTF8.GetString(bytes, 0, Math.Min(256, bytes.Length));
        return head.Contains("<svg", StringComparison.OrdinalIgnoreCase)
            || head.TrimStart().StartsWith("<?xml", StringComparison.OrdinalIgnoreCase) && head.Contains("svg", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Mapped den UI-Slider (50..100 % Luma-Toleranz) auf eine sinnvolle
    /// Farbdistanz-Toleranz (5..120). Default 85 → ~50 = sinnvoller Mittelweg.
    /// </summary>
    private static int MapLumaPctToColorDistance(int lumaPct) =>
        lumaPct switch
        {
            <= 50 => 120,   // sehr aggressiv
            <= 70 => 90,
            <= 80 => 70,
            <= 85 => 50,
            <= 90 => 35,
            <= 95 => 25,
            _     => 15
        };

    public async Task SetDarkLogoAsync(byte[] bytes, string contentType,
        bool removeWhiteBackground = false, int whiteTolerancePercent = 70,
        string? recolorHex = null, CancellationToken ct = default)
    {
        if (removeWhiteBackground || !string.IsNullOrWhiteSpace(recolorHex))
        {
            try
            {
                var tol = MapLumaPctToColorDistance(whiteTolerancePercent);
                var r = LogoProcessor.Process(bytes, new LogoProcessor.Options(
                    RemoveBackground: removeWhiteBackground,
                    ColorTolerance: tol,
                    RecolorHex: recolorHex));
                bytes = r.Bytes;
                contentType = "image/png";
            }
            catch (Exception ex) { logger.LogWarning(ex, "Dark logo processing failed."); }
        }

        var row = await db.BrandingSettings.FirstOrDefaultAsync(ct) ?? new BrandingSettings();
        row.LogoDarkBytes = bytes;
        row.LogoDarkContentType = contentType;
        row.UpdatedAt = DateTime.UtcNow;
        row.Version++;
        if (row.Id == default) db.BrandingSettings.Add(row);
        await db.SaveChangesAsync(ct);
        cache.Remove(CacheKey);
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
