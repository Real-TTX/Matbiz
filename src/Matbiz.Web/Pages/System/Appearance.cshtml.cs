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

    [BindProperty] public string AppName { get; set; } = "";
    [BindProperty] public string PrimaryColor { get; set; } = "#1b6ec2";
    [BindProperty] public string AccentColor1 { get; set; } = "#7c3aed";
    [BindProperty] public string AccentColor2 { get; set; } = "#f59e0b";

    [BindProperty] public IFormFile? Logo { get; set; }

    private const long MaxBytes = 2 * 1024 * 1024;

    public async Task OnGetAsync()
    {
        Current = await branding.GetAsync();
        AppName = Current.AppName;
        PrimaryColor = Current.PrimaryColor;
        AccentColor1 = Current.AccentColor1;
        AccentColor2 = Current.AccentColor2;
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        await branding.UpdateMetaAsync(AppName, PrimaryColor, AccentColor1, AccentColor2);
        TempData["StatusMessage"] = "Aussehen gespeichert.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUploadLogoAsync()
    {
        if (Logo is null || Logo.Length == 0) { TempData["StatusMessage"] = "Keine Datei gewählt."; return RedirectToPage(); }
        if (Logo.Length > MaxBytes) { TempData["StatusMessage"] = $"Datei zu groß (max. {MaxBytes / 1024 / 1024} MB)."; return RedirectToPage(); }
        await using var ms = new MemoryStream();
        await Logo.CopyToAsync(ms);
        await branding.SetLogoAsync(ms.ToArray(), Logo.ContentType);
        TempData["StatusMessage"] = "Logo hochgeladen.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostClearLogoAsync()
    {
        await branding.ClearLogoAsync();
        TempData["StatusMessage"] = "Logo entfernt.";
        return RedirectToPage();
    }
}
