using Matbiz.Web.Modules.CustomFields.Models;
using Matbiz.Web.Modules.CustomFields.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Matbiz.Web.Pages.Customers;

[Authorize(Roles = "Admin")]
public class FieldsModel(CustomFieldService fields) : PageModel
{
    private const CustomFieldEntityType Et = CustomFieldEntityType.Contact;

    public List<CustomFieldDefinition> Items { get; private set; } = new();
    public List<CustomFieldSection> Sections { get; private set; } = new();

    [BindProperty(SupportsGet = true)] public string Tab { get; set; } = "felder";

    public async Task OnGetAsync()
    {
        Items = await fields.ListAsync(Et);
        Sections = await fields.ListSectionsAsync(Et);
        if (Tab != "sektionen" && Tab != "felder") Tab = "felder";
    }

    public async Task<IActionResult> OnPostDeleteFieldAsync(Guid id)
    {
        await fields.DeleteAsync(id);
        return RedirectToPage(new { Tab = "felder" });
    }

    public async Task<IActionResult> OnPostDeleteSectionAsync(Guid id)
    {
        await fields.DeleteSectionAsync(id);
        return RedirectToPage(new { Tab = "sektionen" });
    }
}
