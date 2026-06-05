using System.ComponentModel.DataAnnotations;
using Matbiz.Web.Modules.Teams.Models;

namespace Matbiz.Web.Modules.CustomMenu.Models;

public enum CustomMenuVisibility
{
    Global = 0,
    Departments = 1,
    Teams = 2
}

public enum CustomMenuOpenMode
{
    /// <summary>Link öffnet in neuem Browser-Tab.</summary>
    NewTab = 0,
    /// <summary>Normale Navigation im selben Tab.</summary>
    SameTab = 1,
    /// <summary>Wird als IFrame im Content-Bereich der App eingebettet.</summary>
    Embedded = 2
}

public enum CustomMenuContext
{
    /// <summary>Eintrag in der Haupt-Sidebar (Standard).</summary>
    Sidebar = 0,
    /// <summary>Zusätzlicher Tab auf der Kontakt-Detailseite.</summary>
    ContactDetail = 1,
    /// <summary>Zusätzlicher Tab auf der Firmen-Detailseite.</summary>
    CompanyDetail = 2
}

/// <summary>
/// Admin-verwalteter Link. Erscheint je nach Context in der Sidebar oder als
/// zusätzlicher Tab auf Kontakt-/Firmen-Detail. URL unterstützt Platzhalter
/// wie {Phone}, {FirstName} — werden auf den Detail-Seiten substituiert.
/// </summary>
public class CustomMenuItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(100)]
    public string Label { get; set; } = string.Empty;

    [Required, MaxLength(1000)]
    public string Url { get; set; } = string.Empty;

    /// <summary>Bootstrap icon class wie "bi-rocket". Default: bi-box-arrow-up-right.</summary>
    [MaxLength(50)]
    public string? IconClass { get; set; } = "bi-box-arrow-up-right";

    public CustomMenuOpenMode OpenMode { get; set; } = CustomMenuOpenMode.NewTab;

    public CustomMenuContext Context { get; set; } = CustomMenuContext.Sidebar;

    public int SortOrder { get; set; }

    public CustomMenuVisibility Visibility { get; set; } = CustomMenuVisibility.Global;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<CustomMenuItemDepartment> Departments { get; set; } = new();
    public List<CustomMenuItemTeam> Teams { get; set; } = new();
}

public class CustomMenuItemDepartment
{
    public Guid CustomMenuItemId { get; set; }
    public CustomMenuItem CustomMenuItem { get; set; } = default!;
    public Guid DepartmentId { get; set; }
    public Department Department { get; set; } = default!;
}

public class CustomMenuItemTeam
{
    public Guid CustomMenuItemId { get; set; }
    public CustomMenuItem CustomMenuItem { get; set; } = default!;
    public Guid TeamId { get; set; }
    public Team Team { get; set; } = default!;
}
