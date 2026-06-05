using Matbiz.Web.Modules.Articles.Models;
using Matbiz.Web.Modules.Articles.Services;
using Matbiz.Web.Modules.CustomFields.Models;
using Matbiz.Web.Modules.CustomFields.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Matbiz.Web.Pages.Articles;

[Authorize]
public class EditModel(
    ArticleService articles,
    TaxRateService taxRates,
    NumberRangeService numberRanges,
    CustomFieldService fields) : PageModel
{
    private const CustomFieldEntityType Et = CustomFieldEntityType.Article;
    private const long MaxImageBytes = 4 * 1024 * 1024;  // 4 MB

    [BindProperty(SupportsGet = true)] public Guid? Id { get; set; }
    [BindProperty] public Article Input { get; set; } = new();
    [BindProperty] public Dictionary<Guid, string?> CustomValues { get; set; } = new();
    [BindProperty] public IFormFile? Image { get; set; }

    public List<TaxRate> AllTaxRates { get; private set; } = new();
    public List<CustomFieldDefinition> FieldDefinitions { get; private set; } = new();
    public List<CustomFieldSection> FieldSections { get; private set; } = new();
    public bool HasUnsectionedFields => FieldDefinitions.Any(d => d.SectionId is null);
    public bool IsNew => Id is null;
    public string PreviewNumber { get; private set; } = "";

    private async Task LoadAsync()
    {
        AllTaxRates = await taxRates.ListAsync();
        FieldDefinitions = await fields.ListAsync(Et);
        FieldSections = await fields.ListSectionsAsync(Et);
    }

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadAsync();

        if (Id is Guid gid)
        {
            var a = await articles.GetAsync(gid);
            if (a is null) return NotFound();
            Input = a;
            CustomValues = await fields.GetValueMapAsync(Et, gid);
        }
        else
        {
            var nr = await numberRanges.GetByKeyAsync("Article");
            if (nr is not null) PreviewNumber = numberRanges.PreviewNext(nr);

            var def = await taxRates.GetDefaultAsync();
            if (def is not null) Input.TaxRateId = def.Id;
        }
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadAsync();

        if (string.IsNullOrWhiteSpace(Input.Name))
            ModelState.AddModelError(nameof(Input.Name), "Bezeichnung ist Pflicht.");
        if (Input.TaxRateId == Guid.Empty)
            ModelState.AddModelError(nameof(Input.TaxRateId), "Steuersatz wählen.");
        if (Input.NetPrice < 0)
            ModelState.AddModelError(nameof(Input.NetPrice), "Preis darf nicht negativ sein.");

        if (Image is not null && Image.Length > MaxImageBytes)
            ModelState.AddModelError(nameof(Image), $"Bild zu groß (max. {MaxImageBytes / 1024 / 1024} MB).");

        if (!ModelState.IsValid) return Page();

        Guid savedId;
        if (IsNew)
        {
            await ApplyImageAsync(Input);
            await articles.CreateAsync(Input);
            savedId = Input.Id;
        }
        else
        {
            var a = await articles.GetAsync(Id!.Value);
            if (a is null) return NotFound();
            a.Name = Input.Name;
            a.Description = Input.Description;
            a.Type = Input.Type;
            a.Unit = Input.Unit;
            a.NetPrice = Input.NetPrice;
            a.PurchasePrice = Input.PurchasePrice;
            a.TaxRateId = Input.TaxRateId;
            a.Category = Input.Category;
            a.IsActive = Input.IsActive;
            a.SortOrder = Input.SortOrder;
            await ApplyImageAsync(a);
            await articles.UpdateAsync(a);
            savedId = a.Id;
        }

        if (CustomValues.Count > 0)
            await fields.SaveValuesAsync(Et, savedId, CustomValues);

        return RedirectToPage("/Articles/Index");
    }

    public async Task<IActionResult> OnPostClearImageAsync()
    {
        if (Id is not Guid gid) return RedirectToPage();
        var a = await articles.GetAsync(gid);
        if (a is null) return NotFound();
        a.ImageBytes = null;
        a.ImageContentType = null;
        a.ImageVersion++;
        await articles.UpdateAsync(a);
        return RedirectToPage(new { Id = gid });
    }

    private async Task ApplyImageAsync(Article a)
    {
        if (Image is null || Image.Length == 0) return;
        await using var ms = new MemoryStream();
        await Image.CopyToAsync(ms);
        a.ImageBytes = ms.ToArray();
        a.ImageContentType = Image.ContentType;
        a.ImageVersion++;
    }
}
