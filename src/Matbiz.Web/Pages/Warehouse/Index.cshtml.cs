using Matbiz.Web.Modules.Articles.Models;
using Matbiz.Web.Modules.Articles.Services;
using Matbiz.Web.Modules.Warehouse.Models;
using Matbiz.Web.Modules.Warehouse.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Matbiz.Web.Pages.Warehouse;

[Authorize]
public class IndexModel(
    WarehouseService warehouses,
    StockService stock,
    ArticleService articles) : PageModel
{
    [BindProperty(SupportsGet = true)] public Guid? WarehouseId { get; set; }

    public List<Matbiz.Web.Modules.Warehouse.Models.Warehouse> AllWarehouses { get; private set; } = new();
    public Matbiz.Web.Modules.Warehouse.Models.Warehouse? ActiveWarehouse { get; private set; }
    public List<StockRow> Rows { get; private set; } = new();

    public async Task OnGetAsync()
    {
        AllWarehouses = await warehouses.ListAsync();
        ActiveWarehouse = WarehouseId is Guid wid
            ? AllWarehouses.FirstOrDefault(w => w.Id == wid)
            : (await warehouses.GetDefaultAsync()) ?? AllWarehouses.FirstOrDefault();
        if (ActiveWarehouse is null) return;

        var allArticles = await articles.ListAsync(includeInactive: false);
        var levels = await stock.ListByWarehouseAsync(ActiveWarehouse.Id);
        var levelByArticle = levels.ToDictionary(s => s.ArticleId);

        Rows = allArticles
            .OrderBy(a => a.Name)
            .Select(a =>
            {
                var lvl = levelByArticle.GetValueOrDefault(a.Id);
                return new StockRow(a, lvl?.Quantity ?? 0m, lvl?.ReorderLevel);
            })
            .ToList();
    }

    public record StockRow(Article Article, decimal Quantity, decimal? ReorderLevel)
    {
        public bool BelowReorder => ReorderLevel is decimal r && Quantity < r;
    }

}
