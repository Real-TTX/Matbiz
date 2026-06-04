using System.ComponentModel.DataAnnotations;

namespace Matbiz.Web.Modules.Customers.Models;

public enum CustomerGroupKind
{
    Static = 0,
    Dynamic = 1
}

/// <summary>What is grouped — contacts (default) or companies.</summary>
public enum CustomerGroupEntityKind
{
    Contact = 0,
    Company = 1
}

public class CustomerGroup
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public CustomerGroupKind Kind { get; set; } = CustomerGroupKind.Static;

    public CustomerGroupEntityKind EntityKind { get; set; } = CustomerGroupEntityKind.Contact;

    /// <summary>For dynamic groups: JSON-serialized <see cref="CustomerGroupRules"/>.</summary>
    public string? RulesJson { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<CustomerGroupMember> Members { get; set; } = new();
    public List<CompanyGroupMember> CompanyMembers { get; set; } = new();
}

/// <summary>Static membership row for Contact-kind groups.</summary>
public class CustomerGroupMember
{
    public Guid GroupId { get; set; }
    public CustomerGroup Group { get; set; } = default!;

    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = default!;

    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>Static membership row for Company-kind groups.</summary>
public class CompanyGroupMember
{
    public Guid GroupId { get; set; }
    public CustomerGroup Group { get; set; } = default!;

    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = default!;

    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}

// ---------------------------------------------------------------------------
// Rule model — persisted as JSON in CustomerGroup.RulesJson. Kept intentionally
// simple: a flat list of conditions joined by a single combinator (All / Any).
// Nested groups can be added later by recursing on RuleSet — UI just stays flat for now.
// ---------------------------------------------------------------------------

public enum RuleCombinator
{
    All = 0, // AND
    Any = 1  // OR
}

public enum RuleField
{
    Tag = 0,
    Name = 1,
    CompanyName = 2,
    Email = 3,
    City = 4,
    Country = 5,
    Notes = 6,
    CustomField = 7
}

public enum RuleOperator
{
    Contains = 0,
    NotContains = 1,
    Equals = 2,
    NotEquals = 3,
    StartsWith = 4,
    EndsWith = 5,
    IsEmpty = 6,
    IsNotEmpty = 7,
    GreaterThan = 8,
    LessThan = 9
}

public class CustomerGroupRules
{
    public RuleCombinator Combinator { get; set; } = RuleCombinator.All;
    public List<CustomerGroupCondition> Conditions { get; set; } = new();
}

public class CustomerGroupCondition
{
    public RuleField Field { get; set; }

    /// <summary>For <see cref="RuleField.CustomField"/>: the field definition key.</summary>
    public string? CustomFieldKey { get; set; }

    public RuleOperator Operator { get; set; }

    public string? Value { get; set; }
}
