using Matbiz.Web.Modules.CustomFields.Models;
using Matbiz.Web.Modules.CustomFields.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Matbiz.Web.Pages.Admin.ArticleFields;

[Authorize(Roles = "Admin")]
public class EditFieldModel(CustomFieldService fields) : PageModel
{
    private const CustomFieldEntityType Et = CustomFieldEntityType.Article;

    [BindProperty(SupportsGet = true)] public Guid? Id { get; set; }
    [BindProperty] public CustomFieldDefinition Input { get; set; } = new() { EntityType = Et };

    public List<CustomFieldSection> Sections { get; private set; } = new();
    public bool IsNew => Id is null;

    private async Task LoadAsync() => Sections = await fields.ListSectionsAsync(Et);

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadAsync();
        if (Id is Guid gid)
        {
            var def = await fields.GetAsync(gid);
            if (def is null || def.EntityType != Et) return NotFound();
            Input = def;
        }
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadAsync();
        Input.EntityType = Et;

        if (string.IsNullOrWhiteSpace(Input.Key))
            ModelState.AddModelError(nameof(Input.Key), "Schlüssel ist Pflicht.");
        if (string.IsNullOrWhiteSpace(Input.Label))
            ModelState.AddModelError(nameof(Input.Label), "Bezeichnung ist Pflicht.");

        if (Input.Type == CustomFieldType.ValueList)
        {
            if (!Input.GetOptions().Any())
                ModelState.AddModelError(nameof(Input.Options), "Mindestens ein Wert ist Pflicht.");
        }
        else { Input.Options = null; }

        if (!ModelState.IsValid) return Page();
        if (Input.SectionId == Guid.Empty) Input.SectionId = null;

        if (IsNew)
        {
            var all = await fields.ListAsync(Et);
            if (all.Any(x => string.Equals(x.Key, Input.Key, StringComparison.OrdinalIgnoreCase)))
            {
                ModelState.AddModelError(nameof(Input.Key), "Dieser Schlüssel wird bereits verwendet.");
                return Page();
            }
            await fields.CreateAsync(Input);
        }
        else
        {
            var existing = await fields.GetAsync(Id!.Value);
            if (existing is null) return NotFound();
            existing.Key = Input.Key.Trim();
            existing.Label = Input.Label.Trim();
            existing.Type = Input.Type;
            existing.Required = Input.Required;
            existing.SortOrder = Input.SortOrder;
            existing.SectionId = Input.SectionId;
            existing.Options = Input.Options;
            await fields.UpdateAsync(existing);
        }

        return RedirectToPage("/Admin/ArticleFields/Index", new { Tab = "felder" });
    }
}
