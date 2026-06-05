using Matbiz.Web.Modules.Articles.Models;
using Matbiz.Web.Modules.Articles.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Matbiz.Web.Pages.Articles;

[Authorize]
public class PickerModel(ArticleService articles) : PageModel
{
    [BindProperty(SupportsGet = true, Name = "q")] public string? Query { get; set; }
    public List<Article> Items { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var all = await articles.ListAsync(includeInactive: false);
        Items = string.IsNullOrWhiteSpace(Query)
            ? all
            : all.Where(a =>
                  (a.Number?.Contains(Query, StringComparison.OrdinalIgnoreCase) ?? false)
                  || a.Name.Contains(Query, StringComparison.OrdinalIgnoreCase)
                  || (a.Category?.Contains(Query, StringComparison.OrdinalIgnoreCase) ?? false))
              .ToList();
        return Partial("_PickerResults", this);
    }
}
