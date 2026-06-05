using System.ComponentModel.DataAnnotations;
using Matbiz.Web.Modules.CustomFields.Models;
using Matbiz.Web.Modules.CustomFields.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Matbiz.Web.Pages.Customers.Fields;

[Authorize(Roles = "Admin")]
public class EditSectionModel(CustomFieldService fields) : PageModel
{
    private const CustomFieldEntityType Et = CustomFieldEntityType.Contact;

    [BindProperty(SupportsGet = true)] public Guid? Id { get; set; }
    [BindProperty, Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;
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
        return RedirectToPage("/Customers/Fields", new { Tab = "sektionen" });
    }
}
