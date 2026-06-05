using Matbiz.Web.Data;
using Matbiz.Web.Modules.CustomMenu.Services;
using Matbiz.Web.Modules.Teams.Services;
using Matbiz.Web.Modules.Users.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Matbiz.Web.Pages.Admin;

[Authorize(Roles = "Admin")]
public class IndexModel(
    UserManager<ApplicationUser> users,
    TeamService teams,
    DepartmentService departments,
    CustomMenuService customMenu,
    IServiceProvider sp,
    IConfiguration config,
    ILogger<IndexModel> logger) : PageModel
{
    public int UserCount { get; private set; }
    public int ActiveUserCount { get; private set; }
    public int AdminCount { get; private set; }
    public int TeamCount { get; private set; }
    public int DepartmentCount { get; private set; }
    public int CustomMenuItemCount { get; private set; }

    public async Task OnGetAsync()
    {
        var allUsers = users.Users.ToList();
        UserCount = allUsers.Count;
        ActiveUserCount = allUsers.Count(u => u.IsActive);

        AdminCount = 0;
        foreach (var u in allUsers)
            if (await users.IsInRoleAsync(u, Roles.Admin))
                AdminCount++;

        TeamCount = (await teams.ListAsync()).Count;
        DepartmentCount = (await departments.ListAsync()).Count;
        CustomMenuItemCount = (await customMenu.ListAllAsync()).Count;
    }

    /// <summary>
    /// Lädt Demo-Daten für alle Module on-demand. Seeder sind idempotent —
    /// existierende Daten werden nicht überschrieben, nur die jeweilige
    /// Tabelle wird befüllt wenn sie leer ist.
    /// </summary>
    public async Task<IActionResult> OnPostSeedDemoDataAsync()
    {
        try
        {
            await SampleDataSeeder.SeedAsync(sp, config, logger, force: true);
            await ArticleAndDocumentSampleSeeder.SeedAsync(sp, config, logger, force: true);
            await ExtendedSampleSeeder.SeedAsync(sp, config, logger, force: true);
            TempData["StatusMessage"] = "Demo-Daten geladen. Bereits vorhandene Datensätze wurden nicht angetastet.";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Seeding failed");
            TempData["StatusError"] = $"Fehler beim Seeding: {ex.Message}";
        }
        return RedirectToPage();
    }
}
