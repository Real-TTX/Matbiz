using Matbiz.Web.Modules.Customers.Models;
using Matbiz.Web.Modules.Customers.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Matbiz.Web.Pages.Customers;

[Authorize]
public class CreateModel(CustomerService customers, CompanyService companies) : PageModel
{
    [BindProperty] public Customer Input { get; set; } = new();
    public List<Company> AllCompanies { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        AllCompanies = await companies.ListAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        AllCompanies = await companies.ListAsync();
        if (!ModelState.IsValid) return Page();

        if (Input.CompanyId is not null) Input.CompanyName = null;
        var created = await customers.CreateAsync(Input);
        return RedirectToPage("/Customers/Detail", new { id = created.Id });
    }
}
