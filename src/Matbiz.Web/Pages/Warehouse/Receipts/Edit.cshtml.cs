using Matbiz.Web.Modules.Articles.Services;
using Matbiz.Web.Modules.Customers.Services;
using Matbiz.Web.Modules.Warehouse.Models;
using Matbiz.Web.Modules.Warehouse.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Matbiz.Web.Pages.Warehouse.Receipts;

[Authorize]
public class EditModel(
    GoodsReceiptService receipts,
    WarehouseService warehouses,
    ArticleService articles,
    CompanyService companies) : PageModel
{
    [BindProperty(SupportsGet = true)] public Guid? Id { get; set; }

    public GoodsReceipt? Doc { get; private set; }
    public List<Modules.Warehouse.Models.Warehouse> AllWarehouses { get; private set; } = new();
    public List<Modules.Customers.Models.Company> AllCompanies { get; private set; } = new();

    [BindProperty] public DateTime ReceiptDate { get; set; }
    [BindProperty] public Guid WarehouseId { get; set; }
    [BindProperty] public Guid? SupplierCompanyId { get; set; }
    [BindProperty] public string? SupplierReferenceNumber { get; set; }
    [BindProperty] public string? Note { get; set; }
    [BindProperty] public Guid? NewArticleId { get; set; }

    public bool IsNew => Id is null;
    public bool IsDraft => Doc?.Status == GoodsReceiptStatus.Draft;

    private async Task LoadAsync()
    {
        AllWarehouses = await warehouses.ListAsync();
        AllCompanies = await companies.ListAsync();
        if (Id is Guid gid)
        {
            Doc = await receipts.GetAsync(gid);
            if (Doc is not null)
            {
                ReceiptDate = Doc.ReceiptDate;
                WarehouseId = Doc.WarehouseId;
                SupplierCompanyId = Doc.SupplierCompanyId;
                SupplierReferenceNumber = Doc.SupplierReferenceNumber;
                Note = Doc.Note;
            }
        }
        else
        {
            ReceiptDate = DateTime.UtcNow.Date;
            var def = await warehouses.GetDefaultAsync();
            if (def is not null) WarehouseId = def.Id;
        }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadAsync();
        if (!IsNew && Doc is null) return NotFound();
        return Page();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        await LoadAsync();
        if (WarehouseId == Guid.Empty)
        {
            ModelState.AddModelError(nameof(WarehouseId), "Lager wählen.");
            return Page();
        }
        var d = await receipts.CreateDraftAsync(WarehouseId, SupplierCompanyId);
        // ReceiptDate + Header werden direkt nach Create gespeichert
        d.ReceiptDate = DateTime.SpecifyKind(ReceiptDate, DateTimeKind.Utc);
        d.SupplierReferenceNumber = SupplierReferenceNumber;
        d.Note = Note;
        await receipts.UpdateHeaderAsync(d);
        return RedirectToPage(new { Id = d.Id });
    }

    public async Task<IActionResult> OnPostSaveHeaderAsync()
    {
        await LoadAsync();
        if (Doc is null) return NotFound();
        if (!IsDraft) { TempData["StatusError"] = "Nur Entwürfe bearbeitbar."; return RedirectToPage(new { Id }); }
        Doc.ReceiptDate = DateTime.SpecifyKind(ReceiptDate, DateTimeKind.Utc);
        Doc.WarehouseId = WarehouseId;
        Doc.SupplierCompanyId = SupplierCompanyId;
        Doc.SupplierReferenceNumber = SupplierReferenceNumber;
        Doc.Note = Note;
        await receipts.UpdateHeaderAsync(Doc);
        TempData["StatusMessage"] = "Kopfdaten gespeichert.";
        return RedirectToPage(new { Id });
    }

    public async Task<IActionResult> OnPostAddPositionAsync()
    {
        if (Id is null || NewArticleId is null) return RedirectToPage();
        try { await receipts.AddPositionAsync(Id.Value, NewArticleId.Value); }
        catch (InvalidOperationException ex) { TempData["StatusError"] = ex.Message; }
        return RedirectToPage(new { Id });
    }

    public async Task<IActionResult> OnPostUpdatePositionAsync(Guid posId, string description, decimal quantity, decimal? purchasePrice)
    {
        try
        {
            await receipts.UpdatePositionAsync(new GoodsReceiptPosition
            {
                Id = posId,
                DescriptionSnapshot = description,
                Quantity = quantity,
                PurchasePrice = purchasePrice
            });
        }
        catch (InvalidOperationException ex) { TempData["StatusError"] = ex.Message; }
        return RedirectToPage(new { Id });
    }

    public async Task<IActionResult> OnPostDeletePositionAsync(Guid posId)
    {
        try { await receipts.DeletePositionAsync(posId); }
        catch (InvalidOperationException ex) { TempData["StatusError"] = ex.Message; }
        return RedirectToPage(new { Id });
    }

    public async Task<IActionResult> OnPostBookAsync()
    {
        if (Id is null) return RedirectToPage();
        try
        {
            await receipts.BookAsync(Id.Value);
            TempData["StatusMessage"] = "Wareneingang gebucht — Bestände aktualisiert.";
        }
        catch (InvalidOperationException ex) { TempData["StatusError"] = ex.Message; }
        return RedirectToPage(new { Id });
    }
}
