using Matbiz.Web.Modules.Customers.Models;
using Matbiz.Web.Modules.Customers.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Matbiz.Web.Pages.Companies;

[Authorize]
public class CreateModel(CompanyService companies) : PageModel
{
    [BindProperty] public Company Input { get; set; } = new();

    public IActionResult OnGet() => Page();

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(Input.Name))
        {
            ModelState.AddModelError("Input.Name", "Name ist erforderlich.");
            return Page();
        }
        var c = await companies.CreateAsync(Input);
        return RedirectToPage("/Companies/Detail", new { id = c.Id });
    }
}
