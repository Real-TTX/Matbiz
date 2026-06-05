using Matbiz.Web.Data;
using Matbiz.Web.Modules.CustomMenu.Models;
using Matbiz.Web.Modules.CustomMenu.Services;
using Matbiz.Web.Modules.Customers.Models;
using Matbiz.Web.Modules.Customers.Services;
using Matbiz.Web.Modules.Files.Models;
using Matbiz.Web.Modules.Files.Services;
using Matbiz.Web.Modules.Users.Services;
using Matbiz.Web.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Matbiz.Web.Pages.Companies;

[Authorize]
public class DetailModel(
    CompanyService companies,
    TagService tags,
    AttachedFileService attachedFiles,
    UserAdminService userAdmin,
    CustomMenuService customMenu,
    ICurrentUserAccessor currentUser) : PageModel
{
    [BindProperty(SupportsGet = true)] public Guid Id { get; set; }
    [BindProperty(SupportsGet = true, Name = "tab")] public string? Tab { get; set; }
    [BindProperty(SupportsGet = true)] public bool IncludeContacts { get; set; } = true;

    [BindProperty] public Company Input { get; set; } = new();
    [BindProperty] public string? NewTag { get; set; }
    [BindProperty] public string? NewNote { get; set; }
    [BindProperty] public IFormFile? UploadFile { get; set; }

    public Company? Current { get; private set; }
    public List<Tag> AllTags { get; private set; } = new();
    public List<AttachedFile> LibraryFiles { get; private set; } = new();
    public List<CompanyHistoryView> History { get; private set; } = new();
    public Dictionary<string, ApplicationUser> UsersById { get; private set; } = new();

    public string ActorName(string userId) =>
        UsersById.TryGetValue(userId, out var u) ? (u.DisplayName ?? u.Email ?? userId) : userId;

    public string ActiveTab
    {
        get
        {
            var t = Tab?.ToLowerInvariant();
            if (t is "kontakte" or "stammdaten" or "dateien" or "historie") return t;
            if (t is { Length: > 5 } tt && tt.StartsWith("tool-", StringComparison.OrdinalIgnoreCase)) return tt;
            return "historie";
        }
    }

    public List<CustomMenuItem> ToolItems { get; private set; } = new();

    public CustomMenuItem? ActiveTool =>
        ActiveTab.StartsWith("tool-", StringComparison.OrdinalIgnoreCase)
        && Guid.TryParse(ActiveTab["tool-".Length..], out var tid)
            ? ToolItems.FirstOrDefault(t => t.Id == tid) : null;

    public string? ActiveToolUrl => ActiveTool is null
        ? null : CustomMenuService.SubstituteUrl(ActiveTool.Url, BuildPlaceholderMap());

    private Dictionary<string, string?> BuildPlaceholderMap()
    {
        var c = Current;
        if (c is null) return new();
        return new()
        {
            ["Id"] = c.Id.ToString(),
            ["Name"] = c.Name,
            ["Email"] = c.Email,
            ["Phone"] = c.Phone,
            ["Website"] = null, // Property fehlt aktuell — Platzhalter bleibt leer
            ["Industry"] = null,
            ["Street"] = c.Street,
            ["City"] = c.City,
            ["PostalCode"] = c.PostalCode,
            ["Country"] = c.Country,
        };
    }

    public async Task<IActionResult> OnGetAsync()
    {
        Current = await companies.GetAsync(Id);
        if (Current is null) return NotFound();
        AllTags = await tags.ListAsync();
        LibraryFiles = await attachedFiles.ListForOwnerAsync(AttachedFileOwnerType.CompanyLibrary, Id);
        History = await companies.GetHistoryAsync(Id, IncludeContacts);
        UsersById = (await userAdmin.ListAsync()).ToDictionary(u => u.Id, u => u);
        var ctx = await currentUser.GetAsync();
        ToolItems = await customMenu.ListVisibleAsync(ctx.UserId, CustomMenuContext.CompanyDetail);
        Input = Current;
        return Page();
    }

    public async Task<IActionResult> OnPostNoteAsync()
    {
        if (!string.IsNullOrWhiteSpace(NewNote))
            await companies.AddNoteAsync(Id, NewNote);
        return RedirectToPage(new { id = Id, tab = "historie", includeContacts = IncludeContacts });
    }

    public async Task<IActionResult> OnPostEditHistoryAsync(Guid entryId, string? details)
    {
        await companies.UpdateHistoryAsync(entryId, details ?? "");
        return RedirectToPage(new { id = Id, tab = "historie", includeContacts = IncludeContacts });
    }

    public async Task<IActionResult> OnPostDeleteHistoryAsync(Guid entryId)
    {
        await companies.DeleteHistoryAsync(entryId);
        return RedirectToPage(new { id = Id, tab = "historie", includeContacts = IncludeContacts });
    }

    public async Task<IActionResult> OnPostUploadLibraryAsync()
    {
        if (UploadFile is not null && UploadFile.Length > 0)
            await attachedFiles.UploadAsync(AttachedFileOwnerType.CompanyLibrary, Id, UploadFile);
        return RedirectToPage(new { id = Id, tab = "dateien" });
    }

    public async Task<IActionResult> OnPostDeleteLibraryFileAsync(Guid fileId)
    {
        await attachedFiles.DeleteAsync(fileId);
        return RedirectToPage(new { id = Id, tab = "dateien" });
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        var c = await companies.GetAsync(Id);
        if (c is null) return NotFound();
        c.Name = Input.Name;
        c.Description = Input.Description;
        c.Email = Input.Email;
        c.Phone = Input.Phone;
        c.Street = Input.Street;
        c.PostalCode = Input.PostalCode;
        c.City = Input.City;
        c.Country = Input.Country;
        await companies.UpdateAsync(c);
        TempData["StatusMessage"] = "Firma gespeichert.";
        return RedirectToPage(new { id = Id, tab = "stammdaten" });
    }

    public async Task<IActionResult> OnPostAddTagAsync()
    {
        if (!string.IsNullOrWhiteSpace(NewTag))
            await tags.AddToCompanyAsync(Id, NewTag.Trim());
        return RedirectToPage(new { id = Id, tab = Tab });
    }

    public async Task<IActionResult> OnPostRemoveTagAsync(Guid tagId)
    {
        await tags.RemoveFromCompanyAsync(Id, tagId);
        return RedirectToPage(new { id = Id, tab = Tab });
    }

    public async Task<IActionResult> OnPostDeleteAsync()
    {
        await companies.DeleteAsync(Id);
        return RedirectToPage("/Companies/Index");
    }

    public static string Initials(string name)
    {
        var parts = name.Split(new[] { ' ', '.', '-', '_', '&' }, StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 0 ? "?" :
            parts.Length == 1 ? parts[0][..1].ToUpper() :
            (parts[0][0].ToString() + parts[1][0]).ToUpper();
    }
}
