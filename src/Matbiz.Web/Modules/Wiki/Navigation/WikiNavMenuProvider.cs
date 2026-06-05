using Matbiz.Web.Shared.Navigation;

namespace Matbiz.Web.Modules.Wiki.Navigation;

public class WikiNavMenuProvider : INavMenuProvider
{
    public string? ModuleKey => "Wiki";
    public Task<IReadOnlyList<NavMenuEntry>> GetEntriesAsync(NavMenuContext ctx, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<NavMenuEntry>>(new[]
        {
            new NavMenuEntry("wiki", "Wiki", "bi-book", "/Wiki", SortOrder: 60, ActiveOnPrefix: "/Wiki")
        });
}
