using System.ComponentModel.DataAnnotations;
using Matbiz.Web.Modules.Teams.Models;

namespace Matbiz.Web.Modules.Wiki.Models;

public enum WikiVisibility
{
    /// <summary>Visible to any authenticated user.</summary>
    Global = 0,
    /// <summary>Visible only to users in any team belonging to the linked departments.</summary>
    Departments = 1,
    /// <summary>Visible only to members of the linked teams.</summary>
    Teams = 2
}

public class WikiPage
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(120)]
    public string Slug { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public string ContentMarkdown { get; set; } = string.Empty;

    public WikiVisibility Visibility { get; set; } = WikiVisibility.Global;

    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public string? CreatedByUserId { get; set; }
    public string? UpdatedByUserId { get; set; }

    public List<WikiPageDepartment> Departments { get; set; } = new();
    public List<WikiPageTeam> Teams { get; set; } = new();
    public List<WikiPageEditor> Editors { get; set; } = new();
}

public class WikiPageDepartment
{
    public Guid WikiPageId { get; set; }
    public WikiPage WikiPage { get; set; } = default!;
    public Guid DepartmentId { get; set; }
    public Department Department { get; set; } = default!;
}

public class WikiPageTeam
{
    public Guid WikiPageId { get; set; }
    public WikiPage WikiPage { get; set; } = default!;
    public Guid TeamId { get; set; }
    public Team Team { get; set; } = default!;
}

/// <summary>Extra editors beyond Admin + Creator who can modify the page.</summary>
public class WikiPageEditor
{
    public Guid WikiPageId { get; set; }
    public WikiPage WikiPage { get; set; } = default!;

    [Required]
    public string UserId { get; set; } = string.Empty;
}
