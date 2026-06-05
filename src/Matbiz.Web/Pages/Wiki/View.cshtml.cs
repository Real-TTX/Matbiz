using Matbiz.Web.Data;
using Matbiz.Web.Modules.Wiki.Models;
using Matbiz.Web.Modules.Wiki.Services;
using Matbiz.Web.Modules.Users.Services;
using Matbiz.Web.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Matbiz.Web.Pages.Wiki;

[Authorize]
public class ViewModel(
    WikiService wiki,
    UserAdminService userAdmin,
    ICurrentUserAccessor current) : PageModel
{
    [BindProperty(SupportsGet = true)] public string Slug { get; set; } = "";

    public new WikiPage? Page { get; private set; }
    public bool CanEdit { get; private set; }
    public string? CreatedByName { get; private set; }
    public string? UpdatedByName { get; private set; }

    public async Task<IActionResult> OnGetAsync()
    {
        Page = await wiki.GetBySlugAsync(Slug);
        if (Page is null) return NotFound();

        var ctx = await current.GetAsync();
        var isAdmin = User.IsInRole(Roles.Admin);

        if (!await wiki.CanReadAsync(Page, ctx.UserId, isAdmin)) return Forbid();
        CanEdit = wiki.CanWrite(Page, ctx.UserId, isAdmin);

        if (!string.IsNullOrEmpty(Page.CreatedByUserId) || !string.IsNullOrEmpty(Page.UpdatedByUserId))
        {
            var users = await userAdmin.ListAsync();
            CreatedByName = users.FirstOrDefault(u => u.Id == Page.CreatedByUserId)?.DisplayName ?? users.FirstOrDefault(u => u.Id == Page.CreatedByUserId)?.Email;
            UpdatedByName = users.FirstOrDefault(u => u.Id == Page.UpdatedByUserId)?.DisplayName ?? users.FirstOrDefault(u => u.Id == Page.UpdatedByUserId)?.Email;
        }

        return base.Page();
    }
}
