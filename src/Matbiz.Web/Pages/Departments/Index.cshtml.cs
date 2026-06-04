using Matbiz.Web.Modules.Teams.Models;
using Matbiz.Web.Modules.Teams.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Matbiz.Web.Pages.Departments;

[Authorize(Roles = "Admin")]
public class IndexModel(DepartmentService departments) : PageModel
{
    public List<Department> Items { get; private set; } = new();

    [BindProperty] public Department Draft { get; set; } = new();
    [BindProperty] public bool ShowCreate { get; set; }

    public async Task OnGetAsync() => Items = await departments.ListAsync();

    public async Task<IActionResult> OnPostCreateAsync()
    {
        if (string.IsNullOrWhiteSpace(Draft.Name))
        {
            ModelState.AddModelError("Draft.Name", "Name ist erforderlich.");
            ShowCreate = true;
            await OnGetAsync();
            return Page();
        }
        var d = await departments.CreateAsync(Draft);
        return RedirectToPage("/Departments/Detail", new { id = d.Id });
    }
}
