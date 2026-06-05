using Matbiz.Web.Modules.Customers.Models;
using Matbiz.Web.Modules.Customers.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Matbiz.Web.Pages.Customers.Groups;

[Authorize]
public class CreateModel(CustomerGroupService groups) : PageModel
{
    [BindProperty] public CustomerGroup Input { get; set; } = new();

    public IActionResult OnGet() => Page();

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(Input.Name))
        {
            ModelState.AddModelError("Input.Name", "Name ist erforderlich.");
            return Page();
        }
        var created = await groups.CreateAsync(Input);
        return RedirectToPage("/Customers/Groups/Detail", new { id = created.Id });
    }
}
