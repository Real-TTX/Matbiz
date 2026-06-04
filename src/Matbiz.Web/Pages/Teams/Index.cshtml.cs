using Matbiz.Web.Modules.Teams.Models;
using Matbiz.Web.Modules.Teams.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Matbiz.Web.Pages.Teams;

[Authorize(Roles = "Admin")]
public class IndexModel(TeamService teams) : PageModel
{
    public List<Team> Items { get; private set; } = new();
    [BindProperty] public Team Draft { get; set; } = new();
    [BindProperty] public bool ShowCreate { get; set; }

    public async Task OnGetAsync() => Items = await teams.ListAsync();

    public async Task<IActionResult> OnPostCreateAsync()
    {
        if (string.IsNullOrWhiteSpace(Draft.Name))
        {
            ModelState.AddModelError("Draft.Name", "Name ist erforderlich.");
            ShowCreate = true;
            await OnGetAsync();
            return Page();
        }
        var created = await teams.CreateAsync(Draft);
        return RedirectToPage("/Teams/Detail", new { id = created.Id });
    }
}
