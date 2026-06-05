using Matbiz.Web.Modules.CustomMenu.Models;
using Matbiz.Web.Modules.CustomMenu.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Matbiz.Web.Pages.Admin.Menu;

[Authorize(Roles = "Admin")]
public class IndexModel(CustomMenuService customMenu) : PageModel
{
    public List<CustomMenuItem> SidebarItems { get; private set; } = new();
    public List<CustomMenuItem> ContactItems { get; private set; } = new();
    public List<CustomMenuItem> CompanyItems { get; private set; } = new();

    [BindProperty(SupportsGet = true)] public string Tab { get; set; } = "sidebar";

    public async Task OnGetAsync()
    {
        var all = await customMenu.ListAllAsync();
        SidebarItems = all.Where(i => i.Context == CustomMenuContext.Sidebar).ToList();
        ContactItems = all.Where(i => i.Context == CustomMenuContext.ContactDetail).ToList();
        CompanyItems = all.Where(i => i.Context == CustomMenuContext.CompanyDetail).ToList();
        if (Tab is not ("sidebar" or "contact" or "company")) Tab = "sidebar";
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await customMenu.DeleteAsync(id);
        return RedirectToPage(new { Tab });
    }

    public async Task<IActionResult> OnPostReorderAsync([FromBody] ReorderPayload payload)
    {
        if (payload?.Ids is null) return BadRequest();
        await customMenu.ReorderAsync(payload.Ids);
        return new JsonResult(new { ok = true });
    }

    public record ReorderPayload(List<Guid> Ids);

    public static string VisibilityLabel(CustomMenuVisibility v) => v switch
    {
        CustomMenuVisibility.Global => "Global",
        CustomMenuVisibility.Departments => "Abteilungen",
        CustomMenuVisibility.Teams => "Teams",
        _ => v.ToString()
    };

    public static string OpenModeLabel(CustomMenuOpenMode m) => m switch
    {
        CustomMenuOpenMode.NewTab => "neuer Tab",
        CustomMenuOpenMode.SameTab => "selber Tab",
        CustomMenuOpenMode.Embedded => "eingebettet",
        _ => m.ToString()
    };
}
