using Matbiz.Web.Modules.Articles.Models;
using Matbiz.Web.Modules.Articles.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Matbiz.Web.Pages.Articles;

[Authorize]
public class IndexModel(ArticleService articles) : PageModel
{
    public List<Article> Items { get; private set; } = new();

    public async Task OnGetAsync()
    {
        Items = await articles.ListAsync(includeInactive: true);
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await articles.DeleteAsync(id);
        return RedirectToPage();
    }
}
