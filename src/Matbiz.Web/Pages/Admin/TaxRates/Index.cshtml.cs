using Matbiz.Web.Modules.Articles.Models;
using Matbiz.Web.Modules.Articles.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Matbiz.Web.Pages.Admin.TaxRates;

[Authorize(Roles = "Admin")]
public class IndexModel(TaxRateService taxRates) : PageModel
{
    public List<TaxRate> Items { get; private set; } = new();

    public async Task OnGetAsync()
    {
        Items = await taxRates.ListAsync();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        try { await taxRates.DeleteAsync(id); }
        catch (InvalidOperationException ex) { TempData["StatusError"] = ex.Message; }
        return RedirectToPage();
    }
}
