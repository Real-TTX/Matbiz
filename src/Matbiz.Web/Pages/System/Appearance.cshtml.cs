global using BrandingSettings = Matbiz.Web.Modules.SystemSettings.Models.BrandingSettings;
using Matbiz.Web.Modules.SystemSettings.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Matbiz.Web.Pages.System;

[Authorize(Roles = "Admin")]
public class AppearanceModel(BrandingService branding) : PageModel
{
    public BrandingSettings Current { get; private set; } = new();

    [BindProperty(SupportsGet = true)] public string Tab { get; set; } = "aussehen";

    [BindProperty] public string AppName { get; set; } = "";
    [BindProperty] public string PrimaryColor { get; set; } = "#1b6ec2";
    [BindProperty] public string AccentColor1 { get; set; } = "#7c3aed";
    [BindProperty] public string AccentColor2 { get; set; } = "#f59e0b";
    [BindProperty] public int LogoHeightPx { get; set; } = 40;
    [BindProperty] public bool ShowAppNameUnderLogo { get; set; } = true;
    [BindProperty] public string LogoInvertMode { get; set; } = "None";

    /// <summary>Firma-Stammdaten + Bank + PDF — wird im "Firma" Tab editiert.</summary>
    [BindProperty] public BrandingSettings Company { get; set; } = new();

    [BindProperty] public IFormFile? Logo { get; set; }
    [BindProperty] public IFormFile? LogoDark { get; set; }
    [BindProperty] public bool LogoDarkRemoveWhite { get; set; } = true;
    [BindProperty] public string? LogoDarkRecolorHex { get; set; }
    [BindProperty] public bool LogoRemoveWhite { get; set; } = true;
    [BindProperty] public int LogoWhiteTolerance { get; set; } = 85;
    [BindProperty] public string? LogoRecolorHex { get; set; }

    private const long MaxBytes = 2 * 1024 * 1024;

    public async Task OnGetAsync()
    {
        Current = await branding.GetAsync();
        AppName = Current.AppName;
        PrimaryColor = Current.PrimaryColor;
        AccentColor1 = Current.AccentColor1;
        AccentColor2 = Current.AccentColor2;
        LogoHeightPx = Current.LogoHeightPx;
        ShowAppNameUnderLogo = Current.ShowAppNameUnderLogo;
        LogoInvertMode = Current.LogoInvertMode;
        Company = Current;
        if (Tab is not ("aussehen" or "firma" or "pdf")) Tab = "aussehen";
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        await branding.UpdateMetaAsync(AppName, PrimaryColor, AccentColor1, AccentColor2, LogoHeightPx, ShowAppNameUnderLogo, LogoInvertMode);
        TempData["StatusMessage"] = "Aussehen gespeichert.";
        return RedirectToPage(new { Tab = "aussehen" });
    }

    public async Task<IActionResult> OnPostSaveCompanyAsync()
    {
        await branding.UpdateCompanyAsync(Company);
        TempData["StatusMessage"] = "Firma-Stammdaten gespeichert.";
        return RedirectToPage(new { Tab = "firma" });
    }

    public async Task<IActionResult> OnPostSavePdfAsync()
    {
        // PDF-Tab speichert ebenfalls in BrandingSettings (PdfFooterText, DefaultPaymentTerms)
        await branding.UpdateCompanyAsync(Company);
        TempData["StatusMessage"] = "PDF-Einstellungen gespeichert.";
        return RedirectToPage(new { Tab = "pdf" });
    }

    public async Task<IActionResult> OnPostUploadLogoAsync()
    {
        if (Logo is null || Logo.Length == 0) { TempData["StatusMessage"] = "Keine Datei gewählt."; return RedirectToPage(); }
        if (Logo.Length > MaxBytes) { TempData["StatusMessage"] = $"Datei zu groß (max. {MaxBytes / 1024 / 1024} MB)."; return RedirectToPage(); }
        await using var ms = new MemoryStream();
        await Logo.CopyToAsync(ms);
        await branding.SetLogoAsync(ms.ToArray(), Logo.ContentType,
            removeWhiteBackground: LogoRemoveWhite,
            whiteTolerancePercent: LogoWhiteTolerance,
            recolorHex: string.IsNullOrWhiteSpace(LogoRecolorHex) ? null : LogoRecolorHex);
        TempData["StatusMessage"] = "Logo hochgeladen.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostReprocessLogoAsync()
    {
        try
        {
            await branding.ReprocessLogoAsync(LogoRemoveWhite, LogoWhiteTolerance,
                string.IsNullOrWhiteSpace(LogoRecolorHex) ? null : LogoRecolorHex);
            TempData["StatusMessage"] = "Logo neu prozessiert — Vorschau aktualisiert.";
        }
        catch (Exception ex) { TempData["StatusError"] = ex.Message; }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostQuickBlackTransparentAsync()
    {
        try
        {
            await branding.ReprocessLogoAsync(true, 70, "#000000");
            TempData["StatusMessage"] = "Logo auf Schwarz + transparent umgewandelt.";
        }
        catch (Exception ex) { TempData["StatusError"] = ex.Message; }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostQuickWhiteTransparentAsync()
    {
        try
        {
            await branding.ReprocessLogoAsync(true, 70, "#FFFFFF");
            TempData["StatusMessage"] = "Logo auf Weiß + transparent umgewandelt.";
        }
        catch (Exception ex) { TempData["StatusError"] = ex.Message; }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostClearLogoAsync()
    {
        await branding.ClearLogoAsync();
        TempData["StatusMessage"] = "Logo entfernt.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUploadDarkLogoAsync()
    {
        if (LogoDark is null || LogoDark.Length == 0) { TempData["StatusError"] = "Keine Datei gewählt."; return RedirectToPage(); }
        if (LogoDark.Length > MaxBytes) { TempData["StatusError"] = $"Datei zu groß (max. {MaxBytes / 1024 / 1024} MB)."; return RedirectToPage(); }
        await using var ms = new MemoryStream();
        await LogoDark.CopyToAsync(ms);
        await branding.SetDarkLogoAsync(ms.ToArray(), LogoDark.ContentType,
            removeWhiteBackground: LogoDarkRemoveWhite,
            recolorHex: string.IsNullOrWhiteSpace(LogoDarkRecolorHex) ? null : LogoDarkRecolorHex);
        TempData["StatusMessage"] = "Dark-Mode-Logo gespeichert.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostClearDarkLogoAsync()
    {
        await branding.ClearDarkLogoAsync();
        TempData["StatusMessage"] = "Dark-Mode-Logo entfernt — Light-Logo wird wieder für beide Modes verwendet.";
        return RedirectToPage();
    }
}
