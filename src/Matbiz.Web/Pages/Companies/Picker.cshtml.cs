using Matbiz.Web.Modules.Customers.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Matbiz.Web.Pages.Companies;

/// <summary>Search dialog backend for company picking — mirrors Customers/Picker.</summary>
[Authorize]
public class PickerModel(CompanyService companies) : PageModel
{
    [BindProperty(SupportsGet = true, Name = "q")]
    public string? Query { get; set; }

    public List<Matbiz.Web.Modules.Customers.Models.Company> Items { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        Items = await companies.ListAsync(Query);
        return Partial("_PickerResults", this);
    }
}
