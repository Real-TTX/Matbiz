using System.ComponentModel.DataAnnotations;
using Matbiz.Web.Modules.CustomFields.Models;
using Matbiz.Web.Modules.CustomFields.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Matbiz.Web.Pages.Admin.ArticleFields;

[Authorize(Roles = "Admin")]
public class EditSectionModel(CustomFieldService fields) : PageModel
{
    private const CustomFieldEntityType Et = CustomFieldEntityType.Article;

    [BindProperty(SupportsGet = true)] public Guid? Id { get; set; }
    [BindProperty, Required, MaxLength(100)] public string Name { get; set; } = "";
    [BindProperty] public int SortOrder { get; set; }

    public bool IsNew => Id is null;

    public async Task<IActionResult> OnGetAsync()
    {
        if (Id is Guid gid)
        {
            var s = await fields.GetSectionAsync(gid);
            if (s is null || s.EntityType != Et) return NotFound();
            Name = s.Name;
            SortOrder = s.SortOrder;
        }
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            ModelState.AddModelError(nameof(Name), "Name ist Pflicht.");
            return Page();
        }
        if (IsNew) await fields.CreateSectionAsync(Et, Name);
        else await fields.UpdateSectionAsync(Id!.Value, Name);
        return RedirectToPage("/Admin/ArticleFields/Index", new { Tab = "sektionen" });
    }
}
