using System.ComponentModel.DataAnnotations;

namespace Matbiz.Web.Modules.Teams.Models;

public class Department
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<Team> Teams { get; set; } = new();
}
