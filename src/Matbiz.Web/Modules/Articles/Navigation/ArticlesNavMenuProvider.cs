using Matbiz.Web.Shared.Navigation;

namespace Matbiz.Web.Modules.Articles.Navigation;

public class ArticlesNavMenuProvider : INavMenuProvider
{
    public string? ModuleKey => "Articles";
    public Task<IReadOnlyList<NavMenuEntry>> GetEntriesAsync(NavMenuContext ctx, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<NavMenuEntry>>(new[]
        {
            new NavMenuEntry("articles", "Artikel", "bi-box-seam", "/Articles", SortOrder: 30, ActiveOnPrefix: "/Articles")
        });
}
