using Matbiz.Web.Modules.Articles.Models;
using Matbiz.Web.Modules.Articles.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Matbiz.Web.Pages.Admin.TaxRates;

[Authorize(Roles = "Admin")]
public class EditModel(TaxRateService taxRates) : PageModel
{
    [BindProperty(SupportsGet = true)] public Guid? Id { get; set; }
    [BindProperty] public TaxRate Input { get; set; } = new();
    public bool IsNew => Id is null;

    public async Task<IActionResult> OnGetAsync()
    {
        if (Id is Guid gid)
        {
            var t = await taxRates.GetAsync(gid);
            if (t is null) return NotFound();
            Input = t;
        }
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(Input.Name))
            ModelState.AddModelError(nameof(Input.Name), "Name ist Pflicht.");
        if (Input.Percent < 0 || Input.Percent > 100)
            ModelState.AddModelError(nameof(Input.Percent), "Prozent zwischen 0 und 100.");

        if (!ModelState.IsValid) return Page();

        if (IsNew) await taxRates.CreateAsync(Input);
        else
        {
            var t = await taxRates.GetAsync(Id!.Value);
            if (t is null) return NotFound();
            t.Name = Input.Name;
            t.Percent = Input.Percent;
            t.IsDefault = Input.IsDefault;
            t.SortOrder = Input.SortOrder;
            await taxRates.UpdateAsync(t);
        }

        return RedirectToPage("/Admin/TaxRates/Index");
    }
}
