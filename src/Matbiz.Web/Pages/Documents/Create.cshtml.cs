using Matbiz.Web.Modules.Customers.Models;
using Matbiz.Web.Modules.Customers.Services;
using Matbiz.Web.Modules.Documents.Models;
using Matbiz.Web.Modules.Documents.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Matbiz.Web.Pages.Documents;

[Authorize]
public class CreateModel(
    DocumentService documents,
    CustomerService customers,
    CompanyService companies) : PageModel
{
    [BindProperty(SupportsGet = true)] public DocumentType Type { get; set; } = DocumentType.Offer;
    [BindProperty] public Guid? CustomerId { get; set; }
    [BindProperty] public Guid? CompanyId { get; set; }

    public List<Customer> AllCustomers { get; private set; } = new();
    public List<Company> AllCompanies { get; private set; } = new();

    public string TypeLabel => DocumentService.TypeLabel(Type);

    public async Task OnGetAsync()
    {
        AllCustomers = await customers.ListAsync();
        AllCompanies = await companies.ListAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (CustomerId is null && CompanyId is null)
        {
            ModelState.AddModelError("", "Bitte Kontakt oder Firma wählen.");
            await OnGetAsync();
            return Page();
        }
        var doc = await documents.CreateDraftAsync(Type, CustomerId, CompanyId);
        return RedirectToPage("/Documents/Edit", new { Id = doc.Id });
    }
}
