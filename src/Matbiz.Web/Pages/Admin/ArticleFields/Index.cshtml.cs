using Matbiz.Web.Modules.CustomFields.Models;
using Matbiz.Web.Modules.CustomFields.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Matbiz.Web.Pages.Admin.ArticleFields;

[Authorize(Roles = "Admin")]
public class IndexModel(CustomFieldService fields) : PageModel
{
    private const CustomFieldEntityType Et = CustomFieldEntityType.Article;

    public List<CustomFieldDefinition> Items { get; private set; } = new();
    public List<CustomFieldSection> Sections { get; private set; } = new();

    [BindProperty(SupportsGet = true)] public string Tab { get; set; } = "felder";

    public async Task OnGetAsync()
    {
        Items = await fields.ListAsync(Et);
        Sections = await fields.ListSectionsAsync(Et);
        if (Tab is not ("sektionen" or "felder")) Tab = "felder";
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
