using Matbiz.Web.Data;
using Matbiz.Web.Modules.Teams.Services;
using Matbiz.Web.Modules.Users.Services;
using Matbiz.Web.Modules.Wiki.Models;
using Matbiz.Web.Modules.Wiki.Services;
using Matbiz.Web.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Matbiz.Web.Pages.Wiki;

[Authorize]
public class EditModel(
    WikiService wiki,
    DepartmentService departments,
    TeamService teams,
    UserAdminService userAdmin,
    ICurrentUserAccessor current) : PageModel
{
    [BindProperty(SupportsGet = true)] public string? Slug { get; set; }
    [BindProperty(SupportsGet = true, Name = "new")] public bool IsCreateMode { get; set; }

    [BindProperty] public WikiPage Input { get; set; } = new();
    [BindProperty] public List<Guid> DepartmentIds { get; set; } = new();
    [BindProperty] public List<Guid> TeamIds { get; set; } = new();
    [BindProperty] public List<string> EditorIds { get; set; } = new();

    public List<Matbiz.Web.Modules.Teams.Models.Department> AllDepartments { get; private set; } = new();
    public List<Matbiz.Web.Modules.Teams.Models.Team> AllTeams { get; private set; } = new();
    public List<ApplicationUser> AllUsers { get; private set; } = new();
    public bool IsNew => IsCreateMode || string.IsNullOrEmpty(Slug);

    private async Task LoadLookupsAsync()
    {
        AllDepartments = await departments.ListAsync();
        AllTeams = await teams.ListAsync();
        AllUsers = await userAdmin.ListAsync();
    }

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadLookupsAsync();

        if (IsNew)
        {
            Input = new WikiPage();
            return Page();
        }

        var page = await wiki.GetBySlugAsync(Slug!);
        if (page is null) return NotFound();

        var ctx = await current.GetAsync();
        var isAdmin = User.IsInRole(Roles.Admin);
        if (!wiki.CanWrite(page, ctx.UserId, isAdmin)) return Forbid();

        Input = page;
        DepartmentIds = page.Departments.Select(d => d.DepartmentId).ToList();
        TeamIds = page.Teams.Select(t => t.TeamId).ToList();
        EditorIds = page.Editors.Select(e => e.UserId).ToList();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadLookupsAsync();
        if (string.IsNullOrWhiteSpace(Input.Title))
        {
            ModelState.AddModelError("Input.Title", "Titel ist erforderlich.");
            return Page();
        }

        var ctx = await current.GetAsync();
        var isAdmin = User.IsInRole(Roles.Admin);

        WikiPage page;
        if (IsNew)
        {
            page = new WikiPage
            {
                Title = Input.Title,
                Slug = Input.Slug,
                ContentMarkdown = Input.ContentMarkdown ?? "",
                Visibility = Input.Visibility,
                CreatedByUserId = ctx.UserId,
                UpdatedByUserId = ctx.UserId
            };
            ApplyJoins(page);
            page = await wiki.CreateAsync(page);
        }
        else
        {
            page = await wiki.GetBySlugAsync(Slug!) ?? throw new InvalidOperationException();
            if (!wiki.CanWrite(page, ctx.UserId, isAdmin)) return Forbid();

            page.Title = Input.Title;
            page.Slug = Input.Slug;
            page.ContentMarkdown = Input.ContentMarkdown ?? "";
            page.Visibility = Input.Visibility;
            page.UpdatedByUserId = ctx.UserId;

            page.Departments.Clear();
            page.Teams.Clear();
            page.Editors.Clear();
            ApplyJoins(page);

            await wiki.UpdateAsync(page);
        }

        return RedirectToPage("/Wiki/View", new { slug = page.Slug });
    }

    public async Task<IActionResult> OnPostDeleteAsync()
    {
        if (string.IsNullOrEmpty(Slug)) return NotFound();
        var page = await wiki.GetBySlugAsync(Slug);
        if (page is null) return NotFound();
        var ctx = await current.GetAsync();
        if (!wiki.CanWrite(page, ctx.UserId, User.IsInRole(Roles.Admin))) return Forbid();
        await wiki.DeleteAsync(page.Id);
        return RedirectToPage("/Wiki/Index");
    }

    private void ApplyJoins(WikiPage page)
    {
        if (Input.Visibility == WikiVisibility.Departments)
            foreach (var d in DepartmentIds.Distinct())
                page.Departments.Add(new WikiPageDepartment { DepartmentId = d });
        if (Input.Visibility == WikiVisibility.Teams)
            foreach (var t in TeamIds.Distinct())
                page.Teams.Add(new WikiPageTeam { TeamId = t });
        foreach (var uid in EditorIds.Where(u => !string.IsNullOrEmpty(u)).Distinct())
            page.Editors.Add(new WikiPageEditor { UserId = uid });
    }
}
