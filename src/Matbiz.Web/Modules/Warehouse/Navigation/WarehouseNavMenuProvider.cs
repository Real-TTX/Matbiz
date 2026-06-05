using Matbiz.Web.Shared.Navigation;

namespace Matbiz.Web.Modules.Warehouse.Navigation;

public class WarehouseNavMenuProvider : INavMenuProvider
{
    public string? ModuleKey => "Warehouse";

    public Task<IReadOnlyList<NavMenuEntry>> GetEntriesAsync(NavMenuContext ctx, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<NavMenuEntry>>(new[]
        {
            new NavMenuEntry("warehouse",          "Lager",         "bi-box-seam-fill",  "/Warehouse",          SortOrder: 35, ActiveOnPrefix: "/Warehouse"),
            new NavMenuEntry("warehouse:receipts", "Wareneingänge", "bi-truck",          "/Warehouse/Receipts", SortOrder: 36, IsSub: true),
        });
}
