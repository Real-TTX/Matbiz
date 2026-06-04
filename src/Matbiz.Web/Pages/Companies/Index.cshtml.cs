using Matbiz.Web.Data;
using Matbiz.Web.Modules.Customers.Models;
using Matbiz.Web.Modules.Customers.Services;
using Matbiz.Web.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Matbiz.Web.Pages.Companies;

[Authorize]
public class IndexModel(
    CompanyService companies,
    TagService tags,
    UserManager<ApplicationUser> users) : PageModel
{
    public const string ListKey = "companies";

    public static readonly IReadOnlyList<ColumnDef> AllColumns = new[]
    {
        new ColumnDef("name", "Name"),
        new ColumnDef("tags", "Tags"),
        new ColumnDef("email", "E-Mail"),
        new ColumnDef("phone", "Telefon"),
        new ColumnDef("location", "PLZ / Ort"),
        new ColumnDef("country", "Land", DefaultVisible: false),
        new ColumnDef("contacts", "Kontakte", DefaultVisible: false),
    };

    public List<Company> Items { get; private set; } = new();
    public List<Tag> AllTags { get; private set; } = new();
    public ColumnConfig Columns { get; private set; } = default!;

    [BindProperty(SupportsGet = true)] public string? Search { get; set; }
    [BindProperty(SupportsGet = true, Name = "tag")] public List<Guid> FilterTagIds { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        AllTags = await tags.ListAsync();
        Items = await companies.ListAsync(Search, FilterTagIds);

        var user = await users.GetUserAsync(User);
        Columns = ListPreferences.Resolve(user?.ListPreferencesJson, ListKey, AllColumns);

        if (Request.Headers.ContainsKey("HX-Request"))
            return Partial("_CompanyTable", this);

        return Page();
    }

    public async Task<IActionResult> OnPostSaveColumnsAsync(string listKey, List<string>? visibleKeys, bool reset = false)
    {
        var user = await users.GetUserAsync(User);
        if (user is null) return Forbid();
        var keys = reset ? AllColumns.Where(c => c.DefaultVisible).Select(c => c.Key) : (visibleKeys ?? new List<string>());
        user.ListPreferencesJson = ListPreferences.Save(user.ListPreferencesJson, listKey, keys, AllColumns.Select(c => c.Key));
        await users.UpdateAsync(user);
        return RedirectToPage();
    }
}
