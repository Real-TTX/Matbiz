using Matbiz.Web.Modules.Articles.Models;
using Matbiz.Web.Modules.Articles.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Matbiz.Web.Pages.Admin.NumberRanges;

[Authorize(Roles = "Admin")]
public class EditModel(NumberRangeService numberRanges) : PageModel
{
    [BindProperty(SupportsGet = true)] public Guid Id { get; set; }
    [BindProperty] public NumberRange Input { get; set; } = new();
    [BindProperty] public int ResetTo { get; set; }
    [BindProperty] public int? ResetYear { get; set; }

    public string PreviewNumber { get; private set; } = "";

    public async Task<IActionResult> OnGetAsync()
    {
        var nr = await numberRanges.GetAsync(Id);
        if (nr is null) return NotFound();
        Input = nr;
        ResetTo = nr.CurrentValue;
        ResetYear = nr.CurrentYear;
        PreviewNumber = numberRanges.PreviewNext(nr);
        return Page();
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        var nr = await numberRanges.GetAsync(Id);
        if (nr is null) return NotFound();

        // Format-Felder
        nr.Label = Input.Label;
        nr.Prefix = string.IsNullOrWhiteSpace(Input.Prefix) ? null : Input.Prefix.Trim();
        nr.IncludeYear = Input.IncludeYear;
        nr.Separator = string.IsNullOrEmpty(Input.Separator) ? "-" : Input.Separator;
        nr.Digits = Math.Clamp(Input.Digits, 1, 10);
        await numberRanges.UpdateAsync(nr);

        TempData["StatusMessage"] = "Nummernkreis aktualisiert.";
        return RedirectToPage(new { Id });
    }

    public async Task<IActionResult> OnPostResetAsync()
    {
        await numberRanges.ResetAsync(Id, ResetTo, ResetYear);
        TempData["StatusMessage"] = "Zählerstand gesetzt.";
        return RedirectToPage(new { Id });
    }
}
