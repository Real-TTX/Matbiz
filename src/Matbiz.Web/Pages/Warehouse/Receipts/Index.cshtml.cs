using Matbiz.Web.Modules.Warehouse.Models;
using Matbiz.Web.Modules.Warehouse.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Matbiz.Web.Pages.Warehouse.Receipts;

[Authorize]
public class IndexModel(GoodsReceiptService receipts) : PageModel
{
    public List<GoodsReceipt> Items { get; private set; } = new();

    public async Task OnGetAsync()
    {
        Items = await receipts.ListAsync();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await receipts.DeleteDraftAsync(id);
        return RedirectToPage();
    }

    public static string StatusLabel(GoodsReceiptStatus s) => s switch
    {
        GoodsReceiptStatus.Draft     => "Entwurf",
        GoodsReceiptStatus.Booked    => "Gebucht",
        GoodsReceiptStatus.Cancelled => "Storniert",
        _ => s.ToString()
    };
    public static string StatusBadge(GoodsReceiptStatus s) => s switch
    {
        GoodsReceiptStatus.Draft     => "bg-secondary-subtle text-secondary",
        GoodsReceiptStatus.Booked    => "bg-success-subtle text-success",
        GoodsReceiptStatus.Cancelled => "bg-danger-subtle text-danger",
        _ => "bg-secondary-subtle text-secondary"
    };
}
