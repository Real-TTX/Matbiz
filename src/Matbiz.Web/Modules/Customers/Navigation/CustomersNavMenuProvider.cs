using Matbiz.Web.Shared.Navigation;

namespace Matbiz.Web.Modules.Customers.Navigation;

public class CustomersNavMenuProvider : INavMenuProvider
{
    public string? ModuleKey => null;

    public Task<IReadOnlyList<NavMenuEntry>> GetEntriesAsync(NavMenuContext ctx, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<NavMenuEntry>>(new[]
        {
            new NavMenuEntry("core:dashboard", "Dashboard", "bi-speedometer2", "/",                  SortOrder: 1),
            new NavMenuEntry("core:contacts",  "Kontakte",  "bi-person-vcard", "/Customers",         SortOrder: 10, ActiveOnPrefix: "/Customers"),
            new NavMenuEntry("core:groups",    "Gruppen",   "bi-collection",   "/Customers/Groups",  SortOrder: 11, IsSub: true, ActiveOnPrefix: "/Customers/Groups"),
            new NavMenuEntry("core:companies", "Firmen",    "bi-building",     "/Companies",         SortOrder: 12, ActiveOnPrefix: "/Companies"),
        });
}
