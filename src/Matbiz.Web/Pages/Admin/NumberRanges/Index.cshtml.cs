using Matbiz.Web.Modules.Articles.Models;
using Matbiz.Web.Modules.Articles.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Matbiz.Web.Pages.Admin.NumberRanges;

[Authorize(Roles = "Admin")]
public class IndexModel(NumberRangeService numberRanges) : PageModel
{
    public List<NumberRange> Items { get; private set; } = new();
    public Dictionary<Guid, string> Previews { get; private set; } = new();

    public async Task OnGetAsync()
    {
        await numberRanges.EnsureDefaultsAsync();
        Items = await numberRanges.ListAsync();
        foreach (var nr in Items)
            Previews[nr.Id] = numberRanges.PreviewNext(nr);
    }
}
