using Matbiz.Web.Data;
using Matbiz.Web.Modules.CustomMenu.Models;
using Matbiz.Web.Modules.CustomMenu.Services;
using Matbiz.Web.Modules.Customers.Models;
using Matbiz.Web.Modules.Customers.Services;
using Matbiz.Web.Modules.Files.Models;
using Matbiz.Web.Modules.Files.Services;
using Matbiz.Web.Modules.Tasks.Models;
using Matbiz.Web.Modules.Tasks.Services;
using Matbiz.Web.Modules.Users.Services;
using Matbiz.Web.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Matbiz.Web.Pages.Customers;

[Authorize]
public class DetailModel(
    CustomerService customers,
    Matbiz.Web.Modules.CustomFields.Services.CustomFieldService fields,
    TagService tags,
    TaskService tasks,
    UserAdminService userAdmin,
    Matbiz.Web.Modules.Customers.Services.CompanyService companies,
    AttachedFileService attachedFiles,
    CustomMenuService customMenu,
    ICurrentUserAccessor currentUser) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty(SupportsGet = true, Name = "tab")]
    public string? Tab { get; set; }

    public Customer? Customer { get; private set; }
    public List<Matbiz.Web.Modules.CustomFields.Models.CustomFieldDefinition> Definitions { get; private set; } = new();
    public List<Matbiz.Web.Modules.CustomFields.Models.CustomFieldSection> Sections { get; private set; } = new();
    /// <summary>Custom-Field-Werte indexiert nach FieldDefinitionId.</summary>
    public Dictionary<Guid, string?> CustomFieldValueMap { get; private set; } = new();

    /// <summary>True if any field exists without a section — drives whether
    /// the implicit "Eigene Felder" tab shows.</summary>
    public bool HasUnsectionedFields => Definitions.Any(d => d.SectionId is null);
    public List<TaskItem> Tasks { get; private set; } = new();
    public List<Tag> AllTags { get; private set; } = new();
    public Dictionary<string, ApplicationUser> UsersById { get; private set; } = new();
    public List<Matbiz.Web.Modules.Customers.Models.Company> AllCompanies { get; private set; } = new();
    public List<AttachedFile> LibraryFiles { get; private set; } = new();
    /// <summary>Lookup: history-entry id → list of attachments.</summary>
    public Dictionary<Guid, List<AttachedFile>> HistoryAttachments { get; private set; } = new();
    /// <summary>For File-type custom fields: file-id (Guid stored in Value) → AttachedFile metadata.</summary>
    public Dictionary<Guid, AttachedFile> CustomFieldFiles { get; private set; } = new();

    [BindProperty(SupportsGet = true, Name = "q")]
    public string? HistorySearch { get; set; }

    [BindProperty(SupportsGet = true)]
    public string HistorySort { get; set; } = "at_desc";

    public IEnumerable<CustomerHistoryEntry> FilteredHistory
    {
        get
        {
            if (Customer is null) return Enumerable.Empty<CustomerHistoryEntry>();
            IEnumerable<CustomerHistoryEntry> q = Customer.History;
            if (!string.IsNullOrWhiteSpace(HistorySearch))
            {
                var s = HistorySearch.Trim();
                q = q.Where(h =>
                    (h.Action?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (h.Details?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (UsersById.TryGetValue(h.ActorUserId, out var u) &&
                        ((u.DisplayName?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false) ||
                         (u.Email?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false))));
            }
            return HistorySort switch
            {
                "at_asc" => q.OrderBy(h => h.At),
                "action_asc" => q.OrderBy(h => h.Action).ThenByDescending(h => h.At),
                "action_desc" => q.OrderByDescending(h => h.Action).ThenByDescending(h => h.At),
                _ => q.OrderByDescending(h => h.At)
            };
        }
    }

    public string ActorName(string userId) =>
        UsersById.TryGetValue(userId, out var u) ? (u.DisplayName ?? u.Email ?? userId) : userId;

    [BindProperty] public string? NewNote { get; set; }
    [BindProperty] public string? NewTag { get; set; }
    [BindProperty] public Customer? StammDraft { get; set; }
    [BindProperty] public Dictionary<Guid, string?> CustomValues { get; set; } = new();

    public string ActiveTab
    {
        get
        {
            var t = Tab?.ToLowerInvariant();
            if (t == "stammdaten" || t == "felder" || t == "aufgaben" || t == "dateien" || t == "historie")
                return t;
            if (t is { Length: > 8 } s && s.StartsWith("section-", StringComparison.OrdinalIgnoreCase))
                return s;
            if (t is { Length: > 5 } tt && tt.StartsWith("tool-", StringComparison.OrdinalIgnoreCase))
                return tt;
            return "historie";  // default landing tab — summary is in the header already
        }
    }

    public Guid? ActiveSectionId =>
        ActiveTab.StartsWith("section-", StringComparison.OrdinalIgnoreCase)
        && Guid.TryParse(ActiveTab["section-".Length..], out var id) ? id : null;

    /// <summary>Eingebettete Tools (CustomMenuItems mit Context=ContactDetail), für den User sichtbar.</summary>
    public List<CustomMenuItem> ToolItems { get; private set; } = new();

    public CustomMenuItem? ActiveTool =>
        ActiveTab.StartsWith("tool-", StringComparison.OrdinalIgnoreCase)
        && Guid.TryParse(ActiveTab["tool-".Length..], out var tid)
            ? ToolItems.FirstOrDefault(t => t.Id == tid) : null;

    /// <summary>URL mit ersetzten Platzhaltern für das aktive Tool.</summary>
    public string? ActiveToolUrl => ActiveTool is null
        ? null
        : CustomMenuService.SubstituteUrl(ActiveTool.Url, BuildPlaceholderMap());

    private Dictionary<string, string?> BuildPlaceholderMap()
    {
        var c = Customer;
        if (c is null) return new();
        var parts = c.Name?.Split(' ', 2);
        return new()
        {
            ["Id"] = c.Id.ToString(),
            ["FullName"] = c.Name,
            ["Name"] = c.Name,
            ["FirstName"] = parts is { Length: >= 1 } ? parts[0] : c.Name,
            ["LastName"] = parts is { Length: 2 } ? parts[1] : "",
            ["Email"] = c.Email,
            ["Phone"] = c.Phone,
            ["Mobile"] = c.Phone, // kein separates Mobil-Feld — Phone als Fallback
            ["CompanyName"] = c.EffectiveCompanyName,
            ["CustomerNumber"] = c.Id.ToString("N")[..8],
            ["Street"] = c.Street,
            ["City"] = c.City,
            ["PostalCode"] = c.PostalCode,
            ["Country"] = c.Country,
        };
    }

    [BindProperty] public IFormFile? UploadFile { get; set; }
    [BindProperty] public IFormFile? NoteFile { get; set; }
    [BindProperty] public IFormFile? CustomFieldFile { get; set; }
    [BindProperty] public Guid CustomFieldDefId { get; set; }

    private async Task LoadAsync()
    {
        var et = Matbiz.Web.Modules.CustomFields.Models.CustomFieldEntityType.Contact;
        Customer = await customers.GetAsync(Id);
        Definitions = await fields.ListAsync(et);
        Sections = await fields.ListSectionsAsync(et);
        AllTags = await tags.ListAsync();

        var ctx = await currentUser.GetAsync();
        ToolItems = await customMenu.ListVisibleAsync(ctx.UserId, CustomMenuContext.ContactDetail);
        if (Customer is not null)
        {
            CustomFieldValueMap = await fields.GetValueMapAsync(et, Customer.Id);

            Tasks = await tasks.ListByCustomerAsync(Customer.Id);
            UsersById = (await userAdmin.ListAsync()).ToDictionary(u => u.Id, u => u);
            AllCompanies = await companies.ListAsync();
            LibraryFiles = await attachedFiles.ListForOwnerAsync(AttachedFileOwnerType.CustomerLibrary, Customer.Id);

            var histIds = Customer.History.Select(h => h.Id).ToList();
            if (histIds.Count > 0)
            {
                var all = new List<AttachedFile>();
                foreach (var hid in histIds)
                    all.AddRange(await attachedFiles.ListForOwnerAsync(AttachedFileOwnerType.CustomerHistory, hid));
                HistoryAttachments = all.GroupBy(a => a.OwnerId).ToDictionary(g => g.Key, g => g.ToList());
            }

            // Custom-field "File" lookups — value stores AttachedFile.Id (Guid-string)
            var fileDefIds = Definitions.Where(d => d.Type == Matbiz.Web.Modules.CustomFields.Models.CustomFieldType.File).Select(d => d.Id).ToHashSet();
            foreach (var kv in CustomFieldValueMap)
            {
                if (!fileDefIds.Contains(kv.Key) || !Guid.TryParse(kv.Value, out var fid)) continue;
                var f = await attachedFiles.GetAsync(fid);
                if (f is not null) CustomFieldFiles[fid] = f;
            }
        }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadAsync();
        if (Customer is null) return NotFound();
        return Page();
    }

    /// <summary>htmx target: returns just the history table partial. Used by the
    /// search input on the Historie tab to live-filter without a full reload.</summary>
    public async Task<IActionResult> OnGetHistoryRowsAsync()
    {
        await LoadAsync();
        if (Customer is null) return NotFound();
        return Partial("_HistoryTable", this);
    }

    public async Task<IActionResult> OnPostStammdatenAsync()
    {
        await LoadAsync();
        if (Customer is null || StammDraft is null) return NotFound();
        Customer.Name = StammDraft.Name;
        Customer.CompanyId = StammDraft.CompanyId;
        // Freetext company is only meaningful when no structured company is linked.
        Customer.CompanyName = StammDraft.CompanyId is null ? StammDraft.CompanyName : null;
        Customer.Email = StammDraft.Email;
        Customer.Phone = StammDraft.Phone;
        Customer.Street = StammDraft.Street;
        Customer.PostalCode = StammDraft.PostalCode;
        Customer.City = StammDraft.City;
        Customer.Country = StammDraft.Country;
        Customer.Notes = StammDraft.Notes;
        await customers.UpdateAsync(Customer);
        TempData["StatusMessage"] = "Stammdaten gespeichert.";
        return RedirectToPage(new { id = Id, tab = "stammdaten" });
    }

    public async Task<IActionResult> OnPostFelderAsync()
    {
        await LoadAsync();
        if (Customer is null) return NotFound();
        await fields.SaveValuesAsync(Matbiz.Web.Modules.CustomFields.Models.CustomFieldEntityType.Contact, Customer.Id, CustomValues);
        TempData["StatusMessage"] = "Felder gespeichert.";
        return RedirectToPage(new { id = Id, tab = Tab ?? "felder" });
    }

    public async Task<IActionResult> OnPostEditHistoryAsync(Guid entryId, string? details)
    {
        await customers.UpdateHistoryAsync(entryId, details ?? "");
        return RedirectToPage(new { id = Id, tab = "historie" });
    }

    public async Task<IActionResult> OnPostDeleteHistoryAsync(Guid entryId)
    {
        await customers.DeleteHistoryAsync(entryId);
        return RedirectToPage(new { id = Id, tab = "historie" });
    }

    public async Task<IActionResult> OnPostNoteAsync()
    {
        if (string.IsNullOrWhiteSpace(NewNote) && NoteFile is null)
            return RedirectToPage(new { id = Id, tab = "historie" });

        // We need the new history entry's id to attach files to. AddNoteAsync
        // creates the entry inside the customer service. Easiest: reload after,
        // grab the newest entry, attach files.
        var noteText = !string.IsNullOrWhiteSpace(NewNote) ? NewNote :
                       (NoteFile is not null ? $"Anhang: {NoteFile.FileName}" : "");
        await customers.AddNoteAsync(Id, noteText);

        if (NoteFile is not null)
        {
            // Find the entry we just created — there's one note we wrote for
            // this customer just now; latest history row for this customer.
            await LoadAsync();
            var latest = Customer?.History.OrderByDescending(h => h.At).FirstOrDefault(h => h.Action == "Note");
            if (latest is not null)
                await attachedFiles.UploadAsync(AttachedFileOwnerType.CustomerHistory, latest.Id, NoteFile);
        }

        return RedirectToPage(new { id = Id, tab = "historie" });
    }

    public async Task<IActionResult> OnPostUploadLibraryAsync()
    {
        if (UploadFile is not null && UploadFile.Length > 0)
            await attachedFiles.UploadAsync(AttachedFileOwnerType.CustomerLibrary, Id, UploadFile);
        return RedirectToPage(new { id = Id, tab = "dateien" });
    }

    public async Task<IActionResult> OnPostDeleteLibraryFileAsync(Guid fileId)
    {
        await attachedFiles.DeleteAsync(fileId);
        return RedirectToPage(new { id = Id, tab = "dateien" });
    }

    public async Task<IActionResult> OnPostUploadCustomFieldFileAsync()
    {
        if (CustomFieldFile is null || CustomFieldDefId == Guid.Empty)
            return RedirectToPage(new { id = Id, tab = "felder" });

        await LoadAsync();
        if (Customer is null) return NotFound();
        var et = Matbiz.Web.Modules.CustomFields.Models.CustomFieldEntityType.Contact;

        // Replace-Semantik: alte Datei löschen falls vorhanden
        if (CustomFieldValueMap.TryGetValue(CustomFieldDefId, out var oldVal)
            && Guid.TryParse(oldVal, out var oldId))
            await attachedFiles.DeleteAsync(oldId);

        var saved = await attachedFiles.UploadAsync(AttachedFileOwnerType.CustomFieldValue, CustomFieldDefId, CustomFieldFile);
        if (saved is null) return RedirectToPage(new { id = Id, tab = "felder" });

        await fields.SaveValuesAsync(et, Customer.Id, new Dictionary<Guid, string?> { [CustomFieldDefId] = saved.Id.ToString() });
        return RedirectToPage(new { id = Id, tab = "felder" });
    }

    public async Task<IActionResult> OnPostDeleteCustomFieldFileAsync(Guid fieldDefId)
    {
        await LoadAsync();
        if (Customer is null) return NotFound();
        var et = Matbiz.Web.Modules.CustomFields.Models.CustomFieldEntityType.Contact;

        if (CustomFieldValueMap.TryGetValue(fieldDefId, out var oldVal)
            && Guid.TryParse(oldVal, out var oldId))
            await attachedFiles.DeleteAsync(oldId);

        await fields.SaveValuesAsync(et, Customer.Id, new Dictionary<Guid, string?> { [fieldDefId] = null });
        return RedirectToPage(new { id = Id, tab = "felder" });
    }

    public async Task<IActionResult> OnPostAddTagAsync()
    {
        if (!string.IsNullOrWhiteSpace(NewTag))
            await tags.AddToCustomerAsync(Id, NewTag.Trim());
        return RedirectToPage(new { id = Id, tab = Tab });
    }

    public async Task<IActionResult> OnPostRemoveTagAsync(Guid tagId)
    {
        await tags.RemoveFromCustomerAsync(Id, tagId);
        return RedirectToPage(new { id = Id, tab = Tab });
    }

    public async Task<IActionResult> OnPostDeleteAsync()
    {
        await customers.DeleteAsync(Id);
        return RedirectToPage("/Customers/Index");
    }

    public static string Initials(string name)
    {
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 0 ? "?" :
            parts.Length == 1 ? parts[0][..1].ToUpper() :
            (parts[0][0].ToString() + parts[^1][0]).ToUpper();
    }

}
