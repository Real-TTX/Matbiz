using Matbiz.Web.Data;
using Matbiz.Web.Modules.Customers.Models;
using Matbiz.Web.Modules.Customers.Services;
using Matbiz.Web.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Matbiz.Web.Pages.Customers.Groups;

[Authorize]
public class IndexModel(CustomerGroupService groups, UserManager<ApplicationUser> users) : PageModel
{
    public const string ListKey = "contact-groups";

    public static readonly IReadOnlyList<ColumnDef> AllColumns = new[]
    {
        new ColumnDef("name", "Name"),
        new ColumnDef("entity", "Inhalt"),
        new ColumnDef("kind", "Typ"),
        new ColumnDef("description", "Beschreibung"),
        new ColumnDef("members", "Mitglieder"),
        new ColumnDef("updated", "Aktualisiert", DefaultVisible: false),
    };

    public List<CustomerGroup> Items { get; private set; } = new();
    public ColumnConfig Columns { get; private set; } = default!;

    [BindProperty] public CustomerGroup Draft { get; set; } = new();
    [BindProperty] public bool ShowCreate { get; set; }

    public async Task OnGetAsync()
    {
        Items = await groups.ListAsync();
        var user = await users.GetUserAsync(User);
        Columns = ListPreferences.Resolve(user?.ListPreferencesJson, ListKey, AllColumns);
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        if (string.IsNullOrWhiteSpace(Draft.Name))
        {
            ModelState.AddModelError("Draft.Name", "Name ist erforderlich.");
            ShowCreate = true;
            await OnGetAsync();
            return Page();
        }
        var created = await groups.CreateAsync(Draft);
        return RedirectToPage("/Customers/Groups/Detail", new { id = created.Id });
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
