using Matbiz.Web.Data;
using Matbiz.Web.Modules.Customers.Models;
using Matbiz.Web.Modules.Customers.Services;
using Matbiz.Web.Modules.Files.Models;
using Matbiz.Web.Modules.Files.Services;
using Matbiz.Web.Modules.Tasks.Models;
using Matbiz.Web.Modules.Tasks.Services;
using Matbiz.Web.Modules.Users.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Matbiz.Web.Pages.Customers;

[Authorize]
public class DetailModel(
    CustomerService customers,
    CustomerFieldService fields,
    TagService tags,
    TaskService tasks,
    UserAdminService userAdmin,
    Matbiz.Web.Modules.Customers.Services.CompanyService companies,
    AttachedFileService attachedFiles) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty(SupportsGet = true, Name = "tab")]
    public string? Tab { get; set; }

    public Customer? Customer { get; private set; }
    public List<CustomerFieldDefinition> Definitions { get; private set; } = new();
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

    public string ActiveTab => Tab?.ToLowerInvariant() switch
    {
        "felder" => "felder",
        "historie" => "historie",
        "aufgaben" => "aufgaben",
        "dateien" => "dateien",
        _ => "stammdaten"
    };

    [BindProperty] public IFormFile? UploadFile { get; set; }
    [BindProperty] public IFormFile? NoteFile { get; set; }
    [BindProperty] public IFormFile? CustomFieldFile { get; set; }
    [BindProperty] public Guid CustomFieldDefId { get; set; }

    private async Task LoadAsync()
    {
        Customer = await customers.GetAsync(Id);
        Definitions = await fields.ListAsync();
        AllTags = await tags.ListAsync();
        if (Customer is not null)
        {
            Tasks = await tasks.ListByCustomerAsync(Customer.Id);
            UsersById = (await userAdmin.ListAsync()).ToDictionary(u => u.Id, u => u);
            AllCompanies = await companies.ListAsync();
            LibraryFiles = await attachedFiles.ListForOwnerAsync(AttachedFileOwnerType.CustomerLibrary, Customer.Id);

            // History attachments — group by history entry id
            var histIds = Customer.History.Select(h => h.Id).ToList();
            if (histIds.Count > 0)
            {
                var atts = await attachedFiles.ListForOwnerAsync(AttachedFileOwnerType.CustomerHistory, Guid.Empty);
                // The helper filters by ownerId — call once per entry would be N+1.
                // Instead query the table once directly:
                var all = new List<AttachedFile>();
                foreach (var hid in histIds)
                    all.AddRange(await attachedFiles.ListForOwnerAsync(AttachedFileOwnerType.CustomerHistory, hid));
                HistoryAttachments = all.GroupBy(a => a.OwnerId).ToDictionary(g => g.Key, g => g.ToList());
            }

            // Custom-field "File" lookups — value stores AttachedFile.Id (as guid string)
            var fileIds = Customer.CustomFieldValues
                .Where(v => v.FieldDefinition.Type == CustomFieldType.File && Guid.TryParse(v.Value, out _))
                .Select(v => Guid.Parse(v.Value!))
                .ToList();
            foreach (var fid in fileIds)
            {
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
        foreach (var def in Definitions)
        {
            var input = CustomValues.TryGetValue(def.Id, out var v) ? v : null;
            var existing = Customer.CustomFieldValues.FirstOrDefault(x => x.FieldDefinitionId == def.Id);
            if (existing is null)
            {
                if (!string.IsNullOrWhiteSpace(input))
                    Customer.CustomFieldValues.Add(new CustomerFieldValue { FieldDefinitionId = def.Id, Value = input });
            }
            else
            {
                existing.Value = input;
            }
        }
        await customers.UpdateAsync(Customer);
        TempData["StatusMessage"] = "Felder gespeichert.";
        return RedirectToPage(new { id = Id, tab = "felder" });
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

        // If there is already a file value, delete the previous file first (replace semantics).
        var existing = Customer.CustomFieldValues.FirstOrDefault(v => v.FieldDefinitionId == CustomFieldDefId);
        if (existing is not null && Guid.TryParse(existing.Value, out var oldId))
            await attachedFiles.DeleteAsync(oldId);

        var saved = await attachedFiles.UploadAsync(AttachedFileOwnerType.CustomFieldValue, CustomFieldDefId, CustomFieldFile);
        if (saved is null) return RedirectToPage(new { id = Id, tab = "felder" });

        if (existing is null)
            Customer.CustomFieldValues.Add(new CustomerFieldValue { FieldDefinitionId = CustomFieldDefId, Value = saved.Id.ToString() });
        else
            existing.Value = saved.Id.ToString();

        await customers.UpdateAsync(Customer);
        return RedirectToPage(new { id = Id, tab = "felder" });
    }

    public async Task<IActionResult> OnPostDeleteCustomFieldFileAsync(Guid fieldDefId)
    {
        await LoadAsync();
        if (Customer is null) return NotFound();

        var existing = Customer.CustomFieldValues.FirstOrDefault(v => v.FieldDefinitionId == fieldDefId);
        if (existing is not null)
        {
            if (Guid.TryParse(existing.Value, out var oldId))
                await attachedFiles.DeleteAsync(oldId);
            existing.Value = null;
            await customers.UpdateAsync(Customer);
        }
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
