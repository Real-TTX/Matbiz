using Matbiz.Web.Modules.Statistics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Matbiz.Web.Pages.Statistics;

[Authorize]
public class IndexModel(IEnumerable<IStatisticsProvider> providers) : PageModel
{
    public List<IStatisticsProvider> Providers { get; private set; } = new();
    public StatisticsModuleResult? ActiveResult { get; private set; }
    public IStatisticsProvider? ActiveProvider { get; private set; }

    [BindProperty(SupportsGet = true)] public string? Tab { get; set; }

    public async Task OnGetAsync()
    {
        Providers = providers.OrderBy(p => p.SortOrder).ThenBy(p => p.DisplayName).ToList();
        if (Providers.Count == 0) return;

        ActiveProvider = string.IsNullOrEmpty(Tab)
            ? Providers[0]
            : Providers.FirstOrDefault(p => p.ModuleKey == Tab) ?? Providers[0];
        ActiveResult = await ActiveProvider.GetAsync();
    }
}
