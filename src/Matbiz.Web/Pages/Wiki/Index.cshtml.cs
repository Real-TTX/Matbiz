using Matbiz.Web.Data;
using Matbiz.Web.Modules.Wiki.Models;
using Matbiz.Web.Modules.Wiki.Services;
using Matbiz.Web.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Matbiz.Web.Pages.Wiki;

[Authorize]
public class IndexModel(WikiService wiki, ICurrentUserAccessor current) : PageModel
{
    public List<WikiPage> Pages { get; private set; } = new();

    public async Task OnGetAsync()
    {
        var ctx = await current.GetAsync();
        var isAdmin = User.IsInRole(Roles.Admin);
        Pages = await wiki.ListAccessibleAsync(ctx.UserId, isAdmin);
    }

    public static string VisibilityLabel(WikiVisibility v) => v switch
    {
        WikiVisibility.Global => "Global",
        WikiVisibility.Departments => "Abteilungen",
        WikiVisibility.Teams => "Teams",
        _ => v.ToString()
    };

    public static string VisibilityIcon(WikiVisibility v) => v switch
    {
        WikiVisibility.Global => "bi-globe",
        WikiVisibility.Departments => "bi-diagram-3",
        WikiVisibility.Teams => "bi-people-fill",
        _ => "bi-question"
    };
}
