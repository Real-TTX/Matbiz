using Matbiz.Web.Modules.Customers.Models;
using Matbiz.Web.Modules.Customers.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Matbiz.Web.Pages.Customers;

[Authorize(Roles = "Admin")]
public class FieldsModel(CustomerFieldService fields) : PageModel
{
    public List<CustomerFieldDefinition> Items { get; private set; } = new();

    [BindProperty] public CustomerFieldDefinition Draft { get; set; } = new();

    public async Task OnGetAsync() => Items = await fields.ListAsync();

    public async Task<IActionResult> OnPostAddAsync()
    {
        if (string.IsNullOrWhiteSpace(Draft.Key) || string.IsNullOrWhiteSpace(Draft.Label))
        {
            ModelState.AddModelError("", "Schlüssel und Bezeichnung sind Pflicht.");
            Items = await fields.ListAsync();
            return Page();
        }
        await fields.CreateAsync(Draft);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await fields.DeleteAsync(id);
        return RedirectToPage();
    }
}
