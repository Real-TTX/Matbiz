using Matbiz.Web.Modules.CustomMenu.Models;
using Matbiz.Web.Modules.CustomMenu.Services;
using Matbiz.Web.Shared.Navigation;

namespace Matbiz.Web.Modules.CustomMenu.Navigation;

/// <summary>
/// Bringt admin-gepflegte Sidebar-Links als reguläre NavMenuEntries —
/// damit erscheinen sie im selben Layout-Override und können vom Admin
/// frei in beliebige Sektionen einsortiert werden.
/// Default-Section ist leer (= ohne Header). Admin kann's überschreiben.
/// </summary>
public class CustomMenuNavProvider(CustomMenuService menu) : INavMenuProvider
{
    public string? ModuleKey => "CustomMenu";

    public async Task<IReadOnlyList<NavMenuEntry>> GetEntriesAsync(NavMenuContext ctx, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(ctx.UserId)) return Array.Empty<NavMenuEntry>();
        var items = await menu.ListVisibleAsync(ctx.UserId, CustomMenuContext.Sidebar, ct);

        return items.Select((item, idx) =>
        {
            var icon = string.IsNullOrEmpty(item.IconClass) ? "bi-box-arrow-up-right" : item.IconClass;
            var url = item.OpenMode == CustomMenuOpenMode.Embedded
                ? $"/Tools/{item.Id}"
                : item.Url;
            return new NavMenuEntry(
                Key: "custom:" + item.Id,
                Label: item.Label,
                Icon: icon,
                Url: url,
                Section: null,
                SortOrder: 200 + item.SortOrder,
                OpenInNewTab: item.OpenMode == CustomMenuOpenMode.NewTab);
        }).ToList();
    }
}
