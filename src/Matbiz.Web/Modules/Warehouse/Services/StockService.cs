using Matbiz.Web.Data;
using Matbiz.Web.Modules.Articles.Models;
using Matbiz.Web.Modules.Warehouse.Models;
using Microsoft.EntityFrameworkCore;

namespace Matbiz.Web.Modules.Warehouse.Services;

/// <summary>
/// Bestands-Service: liest StockLevels, bucht StockMovements und
/// aktualisiert dabei den StockLevel-Cache atomar (in einer SaveChanges).
/// </summary>
public class StockService(ApplicationDbContext db)
{
    public Task<List<StockLevel>> ListByWarehouseAsync(Guid warehouseId, CancellationToken ct = default) =>
        db.StockLevels.AsNoTracking()
            .Include(s => s.Article)
            .Where(s => s.WarehouseId == warehouseId)
            .OrderBy(s => s.Article.Name)
            .ToListAsync(ct);

    public Task<List<StockLevel>> ListByArticleAsync(Guid articleId, CancellationToken ct = default) =>
        db.StockLevels.AsNoTracking()
            .Include(s => s.Warehouse)
            .Where(s => s.ArticleId == articleId)
            .ToListAsync(ct);

    public async Task<decimal> GetStockAsync(Guid articleId, Guid warehouseId, CancellationToken ct = default)
    {
        var s = await db.StockLevels.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ArticleId == articleId && x.WarehouseId == warehouseId, ct);
        return s?.Quantity ?? 0m;
    }

    /// <summary>Gesamt-Bestand eines Artikels über alle Lager.</summary>
    public async Task<decimal> GetTotalStockAsync(Guid articleId, CancellationToken ct = default)
    {
        return await db.StockLevels.AsNoTracking()
            .Where(s => s.ArticleId == articleId)
            .SumAsync(s => (decimal?)s.Quantity, ct) ?? 0m;
    }

    /// <summary>Bewegung buchen + StockLevel-Cache aktualisieren.</summary>
    public async Task PostMovementAsync(StockMovement movement, CancellationToken ct = default)
    {
        db.StockMovements.Add(movement);

        var level = await db.StockLevels
            .FirstOrDefaultAsync(s => s.ArticleId == movement.ArticleId
                                   && s.WarehouseId == movement.WarehouseId, ct);
        if (level is null)
        {
            level = new StockLevel
            {
                ArticleId = movement.ArticleId,
                WarehouseId = movement.WarehouseId,
                Quantity = 0m
            };
            db.StockLevels.Add(level);
        }
        level.Quantity += movement.Quantity;
        level.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
    }
}
