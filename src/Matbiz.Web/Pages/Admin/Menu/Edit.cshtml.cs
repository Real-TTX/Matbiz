using Matbiz.Web.Modules.CustomMenu.Models;
using Matbiz.Web.Modules.CustomMenu.Services;
using Matbiz.Web.Modules.Teams.Models;
using Matbiz.Web.Modules.Teams.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Matbiz.Web.Pages.Admin.Menu;

[Authorize(Roles = "Admin")]
public class EditModel(
    CustomMenuService customMenu,
    DepartmentService departments,
    TeamService teams) : PageModel
{
    [BindProperty(SupportsGet = true)] public Guid? Id { get; set; }

    /// <summary>Vorbelegung des Context für Neuanlagen — kommt aus ?context=sidebar|contact|company.</summary>
    [BindProperty(SupportsGet = true)] public string? Context { get; set; }

    [BindProperty] public CustomMenuItem Input { get; set; } = new();
    [BindProperty] public List<Guid> DepartmentIds { get; set; } = new();
    [BindProperty] public List<Guid> TeamIds { get; set; } = new();

    public List<Department> AllDepartments { get; private set; } = new();
    public List<Team> AllTeams { get; private set; } = new();
    public bool IsNew => Id is null;

    private async Task LoadLookupsAsync()
    {
        AllDepartments = await departments.ListAsync();
        AllTeams = await teams.ListAsync();
    }

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadLookupsAsync();
        if (Id is Guid gid)
        {
            var item = await customMenu.GetAsync(gid);
            if (item is null) return NotFound();
            Input = item;
            DepartmentIds = item.Departments.Select(d => d.DepartmentId).ToList();
            TeamIds = item.Teams.Select(t => t.TeamId).ToList();
        }
        else
        {
            // Context-Vorauswahl aus QueryString
            Input.Context = Context switch
            {
                "contact" => CustomMenuContext.ContactDetail,
                "company" => CustomMenuContext.CompanyDetail,
                _ => CustomMenuContext.Sidebar
            };
        }
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadLookupsAsync();
        if (string.IsNullOrWhiteSpace(Input.Label) || string.IsNullOrWhiteSpace(Input.Url))
        {
            ModelState.AddModelError("", "Bezeichnung und Ziel-URL sind Pflicht.");
            return Page();
        }

        if (IsNew)
        {
            ApplyJoins(Input);
            await customMenu.CreateAsync(Input);
        }
        else
        {
            var item = await customMenu.GetAsync(Id!.Value);
            if (item is null) return NotFound();
            item.Label = Input.Label;
            item.Url = Input.Url;
            item.IconClass = string.IsNullOrWhiteSpace(Input.IconClass) ? "bi-box-arrow-up-right" : Input.IconClass;
            item.OpenMode = Input.OpenMode;
            item.Context = Input.Context;
            item.Visibility = Input.Visibility;
            item.Departments.Clear();
            item.Teams.Clear();
            ApplyJoins(item);
            await customMenu.UpdateAsync(item);
        }

        var tab = Input.Context switch
        {
            CustomMenuContext.ContactDetail => "contact",
            CustomMenuContext.CompanyDetail => "company",
            _ => "sidebar"
        };
        return RedirectToPage("/Admin/Menu/Index", new { Tab = tab });
    }

    private void ApplyJoins(CustomMenuItem item)
    {
        if (Input.Visibility == CustomMenuVisibility.Departments)
            foreach (var d in DepartmentIds.Distinct())
                item.Departments.Add(new CustomMenuItemDepartment { DepartmentId = d });
        if (Input.Visibility == CustomMenuVisibility.Teams)
            foreach (var t in TeamIds.Distinct())
                item.Teams.Add(new CustomMenuItemTeam { TeamId = t });
    }
}
