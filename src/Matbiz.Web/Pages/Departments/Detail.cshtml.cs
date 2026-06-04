using Matbiz.Web.Modules.Teams.Models;
using Matbiz.Web.Modules.Teams.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Matbiz.Web.Pages.Departments;

[Authorize(Roles = "Admin")]
public class DetailModel(DepartmentService departments) : PageModel
{
    [BindProperty(SupportsGet = true)] public Guid Id { get; set; }
    [BindProperty] public string? Name { get; set; }
    [BindProperty] public string? Description { get; set; }

    public Department? Dept { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        Dept = await departments.GetAsync(Id);
        if (Dept is null) return NotFound();
        Name = Dept.Name;
        Description = Dept.Description;
        return Page();
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        Dept = await departments.GetAsync(Id);
        if (Dept is null) return NotFound();
        Dept.Name = Name ?? Dept.Name;
        Dept.Description = Description;
        await departments.UpdateAsync(Dept);
        TempData["StatusMessage"] = "Abteilung gespeichert.";
        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostDeleteAsync()
    {
        await departments.DeleteAsync(Id);
        return RedirectToPage("/Departments/Index");
    }
}
