using Matbiz.Web.Modules.Customers.Models;
using Matbiz.Web.Modules.Customers.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Matbiz.Web.Pages.Customers.Groups;

[Authorize]
public class DetailModel(
    CustomerGroupService groups,
    CustomerService customers,
    CompanyService companies,
    Matbiz.Web.Modules.CustomFields.Services.CustomFieldService fields) : PageModel
{
    [BindProperty(SupportsGet = true)] public Guid Id { get; set; }
    [BindProperty(SupportsGet = true, Name = "tab")] public string? Tab { get; set; }

    public CustomerGroup? Group { get; private set; }
    public List<Customer> ResolvedContacts { get; private set; } = new();
    public List<Company> ResolvedCompanies { get; private set; } = new();
    public List<Customer> AllCustomers { get; private set; } = new();
    public List<Company> AllCompanies { get; private set; } = new();
    public List<Matbiz.Web.Modules.CustomFields.Models.CustomFieldDefinition> CustomFields { get; private set; } = new();

    [BindProperty] public CustomerGroupRules Rules { get; set; } = new();
    [BindProperty] public Guid StaticAddId { get; set; }
    [BindProperty] public string? Name { get; set; }
    [BindProperty] public string? Description { get; set; }

    public bool IsCompanyGroup => Group?.EntityKind == CustomerGroupEntityKind.Company;
    public int MemberCount => IsCompanyGroup ? ResolvedCompanies.Count : ResolvedContacts.Count;

    public string ActiveTab
    {
        get
        {
            var t = Tab?.ToLowerInvariant();
            if (t == "settings" || t == "rules") return "settings";
            return "members";
        }
    }

    private async Task LoadAsync()
    {
        Group = await groups.GetAsync(Id);
        if (Group is null) return;
        CustomFields = await fields.ListAsync(Matbiz.Web.Modules.CustomFields.Models.CustomFieldEntityType.Contact);
        Rules = CustomerGroupService.ParseRules(Group.RulesJson);
        Name = Group.Name;
        Description = Group.Description;

        if (Group.EntityKind == CustomerGroupEntityKind.Company)
        {
            ResolvedCompanies = await groups.ResolveCompanyMembersAsync(Group);
            AllCompanies = await companies.ListAsync();
        }
        else
        {
            ResolvedContacts = await groups.ResolveContactMembersAsync(Group);
            AllCustomers = await customers.ListAsync();
        }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadAsync();
        if (Group is null) return NotFound();
        return Page();
    }

    public async Task<IActionResult> OnPostSettingsAsync()
    {
        Group = await groups.GetAsync(Id);
        if (Group is null) return NotFound();
        Group.Name = Name ?? Group.Name;
        Group.Description = Description;
        await groups.UpdateAsync(Group);
        TempData["StatusMessage"] = "Einstellungen gespeichert.";
        return RedirectToPage(new { id = Id, tab = "settings" });
    }

    public async Task<IActionResult> OnPostAddConditionAsync()
    {
        Group = await groups.GetAsync(Id);
        if (Group is null) return NotFound();
        Rules.Conditions.Add(new CustomerGroupCondition { Field = RuleField.Tag, Operator = RuleOperator.Contains });
        Group.RulesJson = CustomerGroupService.SerializeRules(Rules);
        await groups.UpdateAsync(Group);
        return RedirectToPage(new { id = Id, tab = "settings" });
    }

    public async Task<IActionResult> OnPostRemoveConditionAsync(int idx)
    {
        Group = await groups.GetAsync(Id);
        if (Group is null) return NotFound();
        if (idx >= 0 && idx < Rules.Conditions.Count)
            Rules.Conditions.RemoveAt(idx);
        Group.RulesJson = CustomerGroupService.SerializeRules(Rules);
        await groups.UpdateAsync(Group);
        return RedirectToPage(new { id = Id, tab = "settings" });
    }

    public async Task<IActionResult> OnPostSaveRulesAsync()
    {
        Group = await groups.GetAsync(Id);
        if (Group is null) return NotFound();
        Group.RulesJson = CustomerGroupService.SerializeRules(Rules);
        await groups.UpdateAsync(Group);
        TempData["StatusMessage"] = "Regeln gespeichert.";
        return RedirectToPage(new { id = Id, tab = "settings" });
    }

    public async Task<IActionResult> OnPostAddMemberAsync()
    {
        if (StaticAddId == Guid.Empty) return RedirectToPage(new { id = Id, tab = "members" });
        Group = await groups.GetAsync(Id);
        if (Group is null) return NotFound();
        if (Group.EntityKind == CustomerGroupEntityKind.Company)
            await groups.AddCompanyAsync(Id, StaticAddId);
        else
            await groups.AddCustomerAsync(Id, StaticAddId);
        return RedirectToPage(new { id = Id, tab = "members" });
    }

    public async Task<IActionResult> OnPostRemoveMemberAsync(Guid memberId)
    {
        Group = await groups.GetAsync(Id);
        if (Group is null) return NotFound();
        if (Group.EntityKind == CustomerGroupEntityKind.Company)
            await groups.RemoveCompanyAsync(Id, memberId);
        else
            await groups.RemoveCustomerAsync(Id, memberId);
        return RedirectToPage(new { id = Id, tab = "members" });
    }

    public async Task<IActionResult> OnPostDeleteAsync()
    {
        await groups.DeleteAsync(Id);
        return RedirectToPage("/Customers/Groups/Index");
    }

    public List<(Guid Id, string Label)> AvailableContacts =>
        Group is null ? new() :
        AllCustomers.Where(c => !Group.Members.Any(m => m.CustomerId == c.Id))
            .Select(c => (c.Id, string.IsNullOrEmpty(c.CompanyName) ? c.Name : $"{c.Name} ({c.CompanyName})"))
            .ToList();

    public List<(Guid Id, string Label)> AvailableCompaniesList =>
        Group is null ? new() :
        AllCompanies.Where(c => !Group.CompanyMembers.Any(m => m.CompanyId == c.Id))
            .Select(c => (c.Id, c.Name))
            .ToList();

    public static List<RuleOperator> OperatorsFor(RuleField field) => field switch
    {
        RuleField.Tag => new() { RuleOperator.Contains, RuleOperator.NotContains, RuleOperator.IsEmpty, RuleOperator.IsNotEmpty },
        _ => new() { RuleOperator.Contains, RuleOperator.NotContains, RuleOperator.Equals, RuleOperator.NotEquals,
                     RuleOperator.StartsWith, RuleOperator.EndsWith, RuleOperator.IsEmpty, RuleOperator.IsNotEmpty,
                     RuleOperator.GreaterThan, RuleOperator.LessThan }
    };

    public static string OperatorLabel(RuleOperator op) => op switch
    {
        RuleOperator.Contains => "enthält",
        RuleOperator.NotContains => "enthält nicht",
        RuleOperator.Equals => "ist gleich",
        RuleOperator.NotEquals => "ist ungleich",
        RuleOperator.StartsWith => "beginnt mit",
        RuleOperator.EndsWith => "endet mit",
        RuleOperator.IsEmpty => "ist leer",
        RuleOperator.IsNotEmpty => "ist nicht leer",
        RuleOperator.GreaterThan => "größer als",
        RuleOperator.LessThan => "kleiner als",
        _ => op.ToString()
    };
}
