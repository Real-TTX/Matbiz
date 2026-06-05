using Matbiz.Web.Shared.Navigation;

namespace Matbiz.Web.Modules.Statistics.Navigation;

public class StatisticsNavMenuProvider : INavMenuProvider
{
    public string? ModuleKey => "Statistics";
    public Task<IReadOnlyList<NavMenuEntry>> GetEntriesAsync(NavMenuContext ctx, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<NavMenuEntry>>(new[]
        {
            new NavMenuEntry("statistics", "Statistik", "bi-bar-chart", "/Statistics", SortOrder: 50, ActiveOnPrefix: "/Statistics")
        });
}
