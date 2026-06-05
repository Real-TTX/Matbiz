using Matbiz.Web.Modules.Documents.Models;
using Matbiz.Web.Modules.Documents.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Matbiz.Web.Pages.Documents;

[Authorize]
public class IndexModel(DocumentService documents) : PageModel
{
    public List<Document> Items { get; private set; } = new();

    public async Task OnGetAsync()
    {
        Items = await documents.ListAsync();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await documents.DeleteAsync(id);
        return RedirectToPage();
    }

    public static string TypeLabel(DocumentType t) => DocumentService.TypeLabel(t);
    public static string StatusLabel(DocumentStatus s) => DocumentService.StatusLabel(s);
    public static string StatusBadgeClass(DocumentStatus s) => DocumentService.StatusBadgeClass(s);
}
