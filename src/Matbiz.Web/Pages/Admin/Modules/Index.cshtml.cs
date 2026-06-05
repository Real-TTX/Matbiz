using Matbiz.Web.Modules.Modules.Models;
using Matbiz.Web.Modules.Modules.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Matbiz.Web.Pages.Admin.Modules;

[Authorize(Roles = "Admin")]
public class IndexModel(ModuleRegistry registry) : PageModel
{
    public List<(ModuleDefinition Def, bool Enabled)> Items { get; private set; } = new();

    public void OnGet()
    {
        Items = ModuleRegistry.AllModules
            .OrderBy(m => m.SortOrder)
            .Select(m => (m, registry.IsEnabled(m.Key)))
            .ToList();
    }

    public async Task<IActionResult> OnPostToggleAsync(string key)
    {
        var current = registry.IsEnabled(key);
        await registry.SetEnabledAsync(key, !current);
        return RedirectToPage();
    }
}
