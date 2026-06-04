using System.ComponentModel.DataAnnotations;

namespace Matbiz.Web.Modules.Tasks.Models;

public enum TaskStatus
{
    Open = 0,
    InProgress = 1,
    Done = 2,
    Cancelled = 3
}

public enum TaskPriority
{
    Low = 0,
    Normal = 1,
    High = 2,
    Urgent = 3
}

public class TaskItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public TaskStatus Status { get; set; } = TaskStatus.Open;

    public TaskPriority Priority { get; set; } = TaskPriority.Normal;

    public DateTime? DueDate { get; set; }

    /// <summary>User the task belongs to / is assigned to. Mutually exclusive with <see cref="AssignedTeamId"/> in normal use, but not enforced.</summary>
    public string? AssignedUserId { get; set; }

    /// <summary>Team the task is assigned to — every team member sees it in their shared list.</summary>
    public Guid? AssignedTeamId { get; set; }

    /// <summary>Optional reference to a customer (by id, kept loose so modules stay independent).</summary>
    public Guid? CustomerId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public string? CreatedByUserId { get; set; }

    /// <summary>When true (default), completing this task writes an entry to
    /// the linked contact's history. User can untick before saving.</summary>
    public bool LogCompletionToCustomer { get; set; } = true;

    public List<TaskHistoryEntry> History { get; set; } = new();
}
