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

    /// <summary>"all" (default) | "contacts" | "companies"</summary>
    [BindProperty(SupportsGet = true)]
    public string Entity { get; set; } = "all";

    public int CountAll => Items.Count;
    public int CountContacts { get; private set; }
    public int CountCompanies { get; private set; }

    public async Task OnGetAsync()
    {
        var all = await groups.ListAsync();
        CountContacts = all.Count(g => g.EntityKind == CustomerGroupEntityKind.Contact);
        CountCompanies = all.Count(g => g.EntityKind == CustomerGroupEntityKind.Company);

        Items = Entity switch
        {
            "contacts" => all.Where(g => g.EntityKind == CustomerGroupEntityKind.Contact).ToList(),
            "companies" => all.Where(g => g.EntityKind == CustomerGroupEntityKind.Company).ToList(),
            _ => all
        };

        var user = await users.GetUserAsync(User);
        Columns = ListPreferences.Resolve(user?.ListPreferencesJson, ListKey, AllColumns);
    }

    public async Task<IActionResult> OnPostSaveColumnsAsync(string listKey, List<string>? visibleKeys, bool reset = false)
    {
        var user = await users.GetUserAsync(User);
        if (user is null) return Forbid();
        var keys = reset ? AllColumns.Where(c => c.DefaultVisible).Select(c => c.Key) : (visibleKeys ?? new List<string>());
        user.ListPreferencesJson = ListPreferences.Save(user.ListPreferencesJson, listKey, keys, AllColumns.Select(c => c.Key));
        await users.UpdateAsync(user);
        return RedirectToPage(new { entity = Entity });
    }
}
