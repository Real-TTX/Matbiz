using Matbiz.Web.Modules.Articles.Models;
using Matbiz.Web.Modules.Articles.Services;
using Matbiz.Web.Modules.Documents.Models;
using Matbiz.Web.Modules.Documents.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Matbiz.Web.Pages.Documents;

[Authorize]
public class EditModel(
    DocumentService documents,
    ArticleService articles) : PageModel
{
    [BindProperty(SupportsGet = true)] public Guid Id { get; set; }

    public Document? Doc { get; private set; }
    public List<Article> AllArticles { get; private set; } = new();

    // === Kopf-Daten ===
    [BindProperty] public DateTime DocumentDate { get; set; }
    [BindProperty] public DateTime? DueDate { get; set; }
    [BindProperty] public string? HeaderText { get; set; }
    [BindProperty] public string? FooterText { get; set; }
    [BindProperty] public string? PaymentTerms { get; set; }
    [BindProperty] public string? CustomerNameSnapshot { get; set; }
    [BindProperty] public string? CustomerAddressSnapshot { get; set; }

    // === ZUGFeRD / XRechnung Pflicht/Zusatz-Felder ===
    [BindProperty] public DateTime? ServiceDate { get; set; }
    [BindProperty] public string CurrencyCode { get; set; } = "EUR";
    [BindProperty] public string? BuyerOrderNumber { get; set; }
    [BindProperty] public string? ContractNumber { get; set; }
    [BindProperty] public string? BuyerReference { get; set; }
    [BindProperty] public string? BuyerVatIdSnapshot { get; set; }

    // === Positions-Add ===
    [BindProperty] public Guid? NewArticleId { get; set; }

    public string TypeLabel => Doc is null ? "" : DocumentService.TypeLabel(Doc.Type);
    public string StatusLabel => Doc is null ? "" : DocumentService.StatusLabel(Doc.Status);
    public string StatusBadgeClass => Doc is null ? "" : DocumentService.StatusBadgeClass(Doc.Status);
    public DocumentType[] NextTypes => Doc is null ? Array.Empty<DocumentType>() : DocumentService.NextTypes(Doc.Type);

    private async Task LoadAsync()
    {
        Doc = await documents.GetAsync(Id);
        AllArticles = await articles.ListAsync(includeInactive: false);
        if (Doc is not null)
        {
            DocumentDate = Doc.DocumentDate;
            DueDate = Doc.DueDate;
            HeaderText = Doc.HeaderText;
            FooterText = Doc.FooterText;
            PaymentTerms = Doc.PaymentTerms;
            CustomerNameSnapshot = Doc.CustomerNameSnapshot;
            CustomerAddressSnapshot = Doc.CustomerAddressSnapshot;
            ServiceDate = Doc.ServiceDate;
            CurrencyCode = Doc.CurrencyCode;
            BuyerOrderNumber = Doc.BuyerOrderNumber;
            ContractNumber = Doc.ContractNumber;
            BuyerReference = Doc.BuyerReference;
            BuyerVatIdSnapshot = Doc.BuyerVatIdSnapshot;
        }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadAsync();
        if (Doc is null) return NotFound();
        return Page();
    }

    public async Task<IActionResult> OnPostSaveHeaderAsync()
    {
        await LoadAsync();
        if (Doc is null) return NotFound();
        if (Doc.Status != DocumentStatus.Draft)
        {
            TempData["StatusError"] = "Nicht-Entwurf-Belege können nicht mehr geändert werden. Bitte zuerst auf Entwurf zurücksetzen.";
            return RedirectToPage(new { Id });
        }

        Doc.DocumentDate = DateTime.SpecifyKind(DocumentDate, DateTimeKind.Utc);
        Doc.DueDate = DueDate.HasValue ? DateTime.SpecifyKind(DueDate.Value, DateTimeKind.Utc) : null;
        Doc.ServiceDate = ServiceDate.HasValue ? DateTime.SpecifyKind(ServiceDate.Value, DateTimeKind.Utc) : null;
        Doc.HeaderText = HeaderText;
        Doc.FooterText = FooterText;
        Doc.PaymentTerms = PaymentTerms;
        Doc.CustomerNameSnapshot = CustomerNameSnapshot;
        Doc.CustomerAddressSnapshot = CustomerAddressSnapshot;
        Doc.CurrencyCode = string.IsNullOrWhiteSpace(CurrencyCode) ? "EUR" : CurrencyCode.Trim().ToUpperInvariant();
        Doc.BuyerOrderNumber = BuyerOrderNumber;
        Doc.ContractNumber = ContractNumber;
        Doc.BuyerReference = BuyerReference;
        Doc.BuyerVatIdSnapshot = BuyerVatIdSnapshot;

        await documents.UpdateHeaderAsync(Doc);
        TempData["StatusMessage"] = "Beleg gespeichert.";
        return RedirectToPage(new { Id });
    }

    public async Task<IActionResult> OnPostAddPositionAsync()
    {
        await documents.AddPositionAsync(Id, NewArticleId);
        return RedirectToPage(new { Id });
    }

    public async Task<IActionResult> OnPostUpdatePositionAsync(
        Guid posId, string description, string unit,
        decimal quantity, decimal netPrice, decimal discountPercent, decimal taxRatePercent)
    {
        await documents.UpdatePositionAsync(new DocumentPosition
        {
            Id = posId,
            Description = description,
            Unit = unit,
            Quantity = quantity,
            NetPrice = netPrice,
            DiscountPercent = discountPercent,
            TaxRatePercent = taxRatePercent
        });
        return RedirectToPage(new { Id });
    }

    public async Task<IActionResult> OnPostDeletePositionAsync(Guid posId)
    {
        await documents.DeletePositionAsync(posId);
        return RedirectToPage(new { Id });
    }

    public async Task<IActionResult> OnPostSetStatusAsync(DocumentStatus status)
    {
        await documents.SetStatusAsync(Id, status);
        return RedirectToPage(new { Id });
    }

    public async Task<IActionResult> OnPostConvertAsync(DocumentType targetType)
    {
        var newDoc = await documents.ConvertAsync(Id, targetType);
        TempData["StatusMessage"] = $"{DocumentService.TypeLabel(targetType)} erstellt: {newDoc.Number}";
        return RedirectToPage(new { Id = newDoc.Id });
    }
}
