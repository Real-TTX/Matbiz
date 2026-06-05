using System.Text.Json;
using Matbiz.Web.Data;
using Matbiz.Web.Modules.Customers.Models;
using Microsoft.EntityFrameworkCore;

namespace Matbiz.Web.Modules.Customers.Services;

public class CustomerGroupService(ApplicationDbContext db)
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = false };

    public Task<List<CustomerGroup>> ListAsync(CancellationToken ct = default) =>
        db.CustomerGroups.AsNoTracking()
            .Include(g => g.Members)
            .Include(g => g.CompanyMembers)
            .OrderBy(g => g.Name)
            .ToListAsync(ct);

    public Task<CustomerGroup?> GetAsync(Guid id, CancellationToken ct = default) =>
        db.CustomerGroups
            .Include(g => g.Members).ThenInclude(m => m.Customer)
            .Include(g => g.CompanyMembers).ThenInclude(m => m.Company)
            .FirstOrDefaultAsync(g => g.Id == id, ct);

    public async Task<CustomerGroup> CreateAsync(CustomerGroup group, CancellationToken ct = default)
    {
        group.CreatedAt = group.UpdatedAt = DateTime.UtcNow;
        db.CustomerGroups.Add(group);
        await db.SaveChangesAsync(ct);
        return group;
    }

    public async Task UpdateAsync(CustomerGroup group, CancellationToken ct = default)
    {
        group.UpdatedAt = DateTime.UtcNow;
        db.CustomerGroups.Update(group);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var g = await db.CustomerGroups.FindAsync([id], ct);
        if (g is null) return;
        db.CustomerGroups.Remove(g);
        await db.SaveChangesAsync(ct);
    }

    // --- Static contact membership ----------------------------------------
    public async Task AddCustomerAsync(Guid groupId, Guid customerId, CancellationToken ct = default)
    {
        var exists = await db.CustomerGroupMembers.AnyAsync(m => m.GroupId == groupId && m.CustomerId == customerId, ct);
        if (exists) return;
        db.CustomerGroupMembers.Add(new CustomerGroupMember { GroupId = groupId, CustomerId = customerId });
        await db.SaveChangesAsync(ct);
    }

    public async Task RemoveCustomerAsync(Guid groupId, Guid customerId, CancellationToken ct = default)
    {
        var m = await db.CustomerGroupMembers.FirstOrDefaultAsync(x => x.GroupId == groupId && x.CustomerId == customerId, ct);
        if (m is null) return;
        db.CustomerGroupMembers.Remove(m);
        await db.SaveChangesAsync(ct);
    }

    // --- Static company membership ----------------------------------------
    public async Task AddCompanyAsync(Guid groupId, Guid companyId, CancellationToken ct = default)
    {
        var exists = await db.CompanyGroupMembers.AnyAsync(m => m.GroupId == groupId && m.CompanyId == companyId, ct);
        if (exists) return;
        db.CompanyGroupMembers.Add(new CompanyGroupMember { GroupId = groupId, CompanyId = companyId });
        await db.SaveChangesAsync(ct);
    }

    public async Task RemoveCompanyAsync(Guid groupId, Guid companyId, CancellationToken ct = default)
    {
        var m = await db.CompanyGroupMembers.FirstOrDefaultAsync(x => x.GroupId == groupId && x.CompanyId == companyId, ct);
        if (m is null) return;
        db.CompanyGroupMembers.Remove(m);
        await db.SaveChangesAsync(ct);
    }

    // --- Rule (de)serialization --------------------------------------------
    public static CustomerGroupRules ParseRules(string? json) =>
        string.IsNullOrWhiteSpace(json) ? new CustomerGroupRules() :
        JsonSerializer.Deserialize<CustomerGroupRules>(json, Json) ?? new CustomerGroupRules();

    public static string SerializeRules(CustomerGroupRules rules) => JsonSerializer.Serialize(rules, Json);

    // --- Member resolution --------------------------------------------------
    public async Task<List<Customer>> ResolveContactMembersAsync(CustomerGroup group, CancellationToken ct = default)
    {
        if (group.EntityKind != CustomerGroupEntityKind.Contact) return new();

        if (group.Kind == CustomerGroupKind.Static)
        {
            return await db.CustomerGroupMembers.AsNoTracking()
                .Where(m => m.GroupId == group.Id)
                .Select(m => m.Customer)
                .OrderBy(c => c.Name)
                .ToListAsync(ct);
        }

        var rules = ParseRules(group.RulesJson);
        if (rules.Conditions.Count == 0) return new();

        var all = await db.Customers.AsNoTracking()
            .Include(c => c.Tags).ThenInclude(t => t.Tag)
            .Include(c => c.Company)
            .ToListAsync(ct);

        // Custom-Field-Werte für alle Kontakte als Lookup laden — wird vom Evaluator benötigt.
        var contactEt = Matbiz.Web.Modules.CustomFields.Models.CustomFieldEntityType.Contact;
        var rawVals = await db.CustomFieldValues.AsNoTracking()
            .Where(v => v.EntityType == contactEt)
            .Join(db.CustomFieldDefinitions.AsNoTracking(),
                v => v.FieldDefinitionId, d => d.Id,
                (v, d) => new { v.EntityId, DefKey = d.Key, v.Value })
            .ToListAsync(ct);
        var valuesByCustomer = rawVals
            .GroupBy(x => x.EntityId)
            .ToDictionary(g => g.Key, g => g.ToDictionary(x => x.DefKey, x => x.Value, StringComparer.OrdinalIgnoreCase));

        return all.Where(c => DynamicGroupEvaluator.MatchesContact(c, rules, valuesByCustomer.GetValueOrDefault(c.Id)))
            .OrderBy(c => c.Name).ToList();
    }

    public async Task<List<Company>> ResolveCompanyMembersAsync(CustomerGroup group, CancellationToken ct = default)
    {
        if (group.EntityKind != CustomerGroupEntityKind.Company) return new();

        if (group.Kind == CustomerGroupKind.Static)
        {
            return await db.CompanyGroupMembers.AsNoTracking()
                .Where(m => m.GroupId == group.Id)
                .Select(m => m.Company)
                .OrderBy(c => c.Name)
                .ToListAsync(ct);
        }

        var rules = ParseRules(group.RulesJson);
        if (rules.Conditions.Count == 0) return new();

        var all = await db.Companies.AsNoTracking()
            .Include(c => c.Tags).ThenInclude(t => t.Tag)
            .ToListAsync(ct);

        return all.Where(c => DynamicGroupEvaluator.MatchesCompany(c, rules)).OrderBy(c => c.Name).ToList();
    }
}

internal static class DynamicGroupEvaluator
{
    public static bool MatchesContact(Customer customer, CustomerGroupRules rules, Dictionary<string, string?>? customValues = null)
    {
        if (rules.Conditions.Count == 0) return false;
        return rules.Combinator == RuleCombinator.All
            ? rules.Conditions.All(c => EvalContact(customer, c, customValues))
            : rules.Conditions.Any(c => EvalContact(customer, c, customValues));
    }

    public static bool MatchesCompany(Company company, CustomerGroupRules rules)
    {
        if (rules.Conditions.Count == 0) return false;
        return rules.Combinator == RuleCombinator.All
            ? rules.Conditions.All(c => EvalCompany(company, c))
            : rules.Conditions.Any(c => EvalCompany(company, c));
    }

    private static bool EvalContact(Customer c, CustomerGroupCondition cond, Dictionary<string, string?>? customValues)
    {
        if (cond.Field == RuleField.Tag)
            return EvalTag(c.Tags.Select(t => t.Tag.Name), cond);

        var actual = cond.Field switch
        {
            RuleField.Name        => c.Name,
            RuleField.CompanyName => c.CompanyName ?? c.Company?.Name,
            RuleField.Email       => c.Email,
            RuleField.City        => c.City,
            RuleField.Country     => c.Country,
            RuleField.Notes       => c.Notes,
            RuleField.CustomField => customValues is not null && cond.CustomFieldKey is not null
                                      && customValues.TryGetValue(cond.CustomFieldKey, out var cv) ? cv : null,
            _ => null
        };
        return EvalString(actual, cond);
    }

    private static bool EvalCompany(Company c, CustomerGroupCondition cond)
    {
        if (cond.Field == RuleField.Tag)
            return EvalTag(c.Tags.Select(t => t.Tag.Name), cond);

        // Custom-field rules don't apply to companies (no custom fields yet) — no match.
        if (cond.Field == RuleField.CustomField) return false;

        var actual = cond.Field switch
        {
            RuleField.Name        => c.Name,
            RuleField.CompanyName => c.Name,
            RuleField.Email       => c.Email,
            RuleField.City        => c.City,
            RuleField.Country     => c.Country,
            RuleField.Notes       => c.Description,
            _ => null
        };
        return EvalString(actual, cond);
    }

    private static bool EvalTag(IEnumerable<string> tagNames, CustomerGroupCondition cond)
    {
        var names = tagNames.ToList();
        var v = (cond.Value ?? "").Trim();
        return cond.Operator switch
        {
            RuleOperator.Contains    => names.Any(n => n.Equals(v, StringComparison.OrdinalIgnoreCase)),
            RuleOperator.NotContains => !names.Any(n => n.Equals(v, StringComparison.OrdinalIgnoreCase)),
            RuleOperator.IsEmpty     => names.Count == 0,
            RuleOperator.IsNotEmpty  => names.Count > 0,
            _                        => false
        };
    }

    private static bool EvalString(string? actual, CustomerGroupCondition cond)
    {
        var target = cond.Value;
        return cond.Operator switch
        {
            RuleOperator.IsEmpty     => string.IsNullOrWhiteSpace(actual),
            RuleOperator.IsNotEmpty  => !string.IsNullOrWhiteSpace(actual),
            RuleOperator.Equals      => string.Equals(actual ?? "", target ?? "", StringComparison.OrdinalIgnoreCase),
            RuleOperator.NotEquals   => !string.Equals(actual ?? "", target ?? "", StringComparison.OrdinalIgnoreCase),
            RuleOperator.Contains    => (actual ?? "").Contains(target ?? "", StringComparison.OrdinalIgnoreCase),
            RuleOperator.NotContains => !(actual ?? "").Contains(target ?? "", StringComparison.OrdinalIgnoreCase),
            RuleOperator.StartsWith  => (actual ?? "").StartsWith(target ?? "", StringComparison.OrdinalIgnoreCase),
            RuleOperator.EndsWith    => (actual ?? "").EndsWith(target ?? "", StringComparison.OrdinalIgnoreCase),
            RuleOperator.GreaterThan => CompareNumeric(actual, target) > 0,
            RuleOperator.LessThan    => CompareNumeric(actual, target) < 0,
            _                        => false
        };
    }

    private static int CompareNumeric(string? a, string? b)
    {
        if (double.TryParse(a, out var x) && double.TryParse(b, out var y))
            return x.CompareTo(y);
        return string.Compare(a ?? "", b ?? "", StringComparison.OrdinalIgnoreCase);
    }
}
