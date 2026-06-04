using Matbiz.Web.Modules.Customers.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Matbiz.Web.Pages.Customers;

/// <summary>
/// Tiny endpoint backing the customer search dialog. Returns the result-list
/// partial — htmx swaps it into the modal body. Always a partial (no layout).
/// </summary>
[Authorize]
public class PickerModel(CustomerService customers) : PageModel
{
    [BindProperty(SupportsGet = true, Name = "q")]
    public string? Query { get; set; }

    public List<Matbiz.Web.Modules.Customers.Models.Customer> Items { get; private set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        Items = await customers.ListAsync(Query, null);
        return Partial("_PickerResults", this);
    }
}
