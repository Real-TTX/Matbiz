using Matbiz.Web.Data;
using Matbiz.Web.Modules.Articles.Models;
using Matbiz.Web.Modules.Articles.Services;
using Matbiz.Web.Modules.Warehouse.Models;
using Matbiz.Web.Shared;
using Microsoft.EntityFrameworkCore;

namespace Matbiz.Web.Modules.Warehouse.Services;

public class GoodsReceiptService(
    ApplicationDbContext db,
    NumberRangeService numbers,
    StockService stock,
    ICurrentUserAccessor current)
{
    public Task<List<GoodsReceipt>> ListAsync(CancellationToken ct = default) =>
        db.GoodsReceipts.AsNoTracking()
            .Include(r => r.Warehouse)
            .Include(r => r.SupplierCompany)
            .OrderByDescending(r => r.ReceiptDate)
            .ThenByDescending(r => r.CreatedAt)
            .ToListAsync(ct);

    public Task<GoodsReceipt?> GetAsync(Guid id, CancellationToken ct = default) =>
        db.GoodsReceipts
            .Include(r => r.Warehouse)
            .Include(r => r.SupplierCompany)
            .Include(r => r.Positions.OrderBy(p => p.Position))
                .ThenInclude(p => p.Article)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<GoodsReceipt> CreateDraftAsync(Guid warehouseId, Guid? supplierId, CancellationToken ct = default)
    {
        var ctx = await current.GetAsync();
        var receipt = new GoodsReceipt
        {
            WarehouseId = warehouseId,
            SupplierCompanyId = supplierId,
            CreatedByUserId = ctx.UserId ?? string.Empty,
            Number = await numbers.NextAsync("GoodsReceipt", ct)
        };
        db.GoodsReceipts.Add(receipt);
        await db.SaveChangesAsync(ct);
        return receipt;
    }

    public async Task UpdateHeaderAsync(GoodsReceipt receipt, CancellationToken ct = default)
    {
        receipt.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task<GoodsReceiptPosition> AddPositionAsync(Guid receiptId, Guid articleId, CancellationToken ct = default)
    {
        var receipt = await db.GoodsReceipts.Include(r => r.Positions)
            .FirstOrDefaultAsync(r => r.Id == receiptId, ct)
            ?? throw new InvalidOperationException("Wareneingang nicht gefunden.");
        if (receipt.Status != GoodsReceiptStatus.Draft)
            throw new InvalidOperationException("Nur Entwürfe können bearbeitet werden.");

        var article = await db.Articles.FindAsync(new object[] { articleId }, ct)
            ?? throw new InvalidOperationException("Artikel nicht gefunden.");

        var nextPos = (receipt.Positions.Count == 0 ? 0 : receipt.Positions.Max(p => p.Position)) + 1;
        var p = new GoodsReceiptPosition
        {
            ReceiptId = receiptId,
            Position = nextPos,
            ArticleId = articleId,
            ArticleNumberSnapshot = article.Number,
            DescriptionSnapshot = article.Name,
            Quantity = 1m,
            PurchasePrice = article.PurchasePrice
        };
        db.GoodsReceiptPositions.Add(p);
        receipt.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return p;
    }

    public async Task UpdatePositionAsync(GoodsReceiptPosition input, CancellationToken ct = default)
    {
        var p = await db.GoodsReceiptPositions.Include(x => x.Receipt)
            .FirstOrDefaultAsync(x => x.Id == input.Id, ct);
        if (p is null) return;
        if (p.Receipt.Status != GoodsReceiptStatus.Draft)
            throw new InvalidOperationException("Nur Entwürfe können bearbeitet werden.");
        p.DescriptionSnapshot = input.DescriptionSnapshot;
        p.Quantity = input.Quantity;
        p.PurchasePrice = input.PurchasePrice;
        await db.SaveChangesAsync(ct);
    }

    public async Task DeletePositionAsync(Guid positionId, CancellationToken ct = default)
    {
        var p = await db.GoodsReceiptPositions.Include(x => x.Receipt)
            .FirstOrDefaultAsync(x => x.Id == positionId, ct);
        if (p is null) return;
        if (p.Receipt.Status != GoodsReceiptStatus.Draft)
            throw new InvalidOperationException("Nur Entwürfe können bearbeitet werden.");
        db.GoodsReceiptPositions.Remove(p);
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Wareneingang buchen: pro Position eine Receipt-Bewegung erzeugen,
    /// StockLevels werden vom StockService gepflegt. Anschließend immutable.</summary>
    public async Task BookAsync(Guid receiptId, CancellationToken ct = default)
    {
        var receipt = await GetAsync(receiptId, ct)
            ?? throw new InvalidOperationException("Wareneingang nicht gefunden.");
        if (receipt.Status != GoodsReceiptStatus.Draft)
            throw new InvalidOperationException("Wareneingang ist nicht im Entwurfsstatus.");
        if (receipt.Positions.Count == 0)
            throw new InvalidOperationException("Keine Positionen — nichts zu buchen.");

        var ctx = await current.GetAsync();
        foreach (var p in receipt.Positions)
        {
            await stock.PostMovementAsync(new StockMovement
            {
                At = receipt.ReceiptDate,
                WarehouseId = receipt.WarehouseId,
                ArticleId = p.ArticleId,
                Type = StockMovementType.Receipt,
                Quantity = p.Quantity,
                Reference = receipt.Number,
                CreatedByUserId = ctx.UserId ?? string.Empty
            }, ct);
        }

        receipt.Status = GoodsReceiptStatus.Booked;
        receipt.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteDraftAsync(Guid receiptId, CancellationToken ct = default)
    {
        var receipt = await db.GoodsReceipts.FindAsync(new object[] { receiptId }, ct);
        if (receipt is null) return;
        if (receipt.Status != GoodsReceiptStatus.Draft) return;  // gebuchte/stornierte nicht löschen
        db.GoodsReceipts.Remove(receipt);
        await db.SaveChangesAsync(ct);
    }
}
