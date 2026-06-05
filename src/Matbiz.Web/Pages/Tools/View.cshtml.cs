using Matbiz.Web.Modules.CustomMenu.Models;
using Matbiz.Web.Modules.CustomMenu.Services;
using Matbiz.Web.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Matbiz.Web.Pages.Tools;

[Authorize]
public class ViewModel(CustomMenuService customMenu, ICurrentUserAccessor current) : PageModel
{
    public CustomMenuItem? Item { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var item = await customMenu.GetAsync(id);
        if (item is null || item.Context != CustomMenuContext.Sidebar
            || item.OpenMode != CustomMenuOpenMode.Embedded) return NotFound();

        // Sichtbarkeit prüfen — gleiche Logik wie ListVisibleAsync.
        var ctx = await current.GetAsync();
        var visible = await customMenu.ListVisibleAsync(ctx.UserId, CustomMenuContext.Sidebar);
        if (!visible.Any(v => v.Id == id)) return Forbid();

        Item = item;
        return Page();
    }
}
