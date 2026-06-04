using Matbiz.Web.Data;
using Matbiz.Web.Modules.Customers.Models;
using Matbiz.Web.Modules.Tasks.Models;
using Matbiz.Web.Shared;
using Microsoft.EntityFrameworkCore;
using TaskStatus = Matbiz.Web.Modules.Tasks.Models.TaskStatus;

namespace Matbiz.Web.Modules.Tasks.Services;

public class TaskService(ApplicationDbContext db, ICurrentUserAccessor currentUser)
{
    public async Task<List<TaskItem>> ListMineAsync(CancellationToken ct = default)
    {
        var ctx = await currentUser.GetAsync();
        return await db.Tasks.AsNoTracking()
            .Where(x => x.AssignedUserId == ctx.UserId)
            .OrderBy(x => x.Status).ThenByDescending(x => x.Priority).ThenBy(x => x.DueDate)
            .ToListAsync(ct);
    }

    public async Task<List<TaskItem>> ListTeamAsync(CancellationToken ct = default)
    {
        var ctx = await currentUser.GetAsync();
        if (ctx.UserId is null) return new();
        return await db.Tasks.AsNoTracking()
            .Where(x => x.AssignedTeamId != null
                        && db.TeamMembers.Any(m => m.TeamId == x.AssignedTeamId && m.UserId == ctx.UserId))
            .OrderBy(x => x.Status).ThenByDescending(x => x.Priority).ThenBy(x => x.DueDate)
            .ToListAsync(ct);
    }

    public Task<List<TaskItem>> ListForCurrentUserAsync(CancellationToken ct = default) => ListMineAsync(ct);

    public Task<TaskItem?> GetAsync(Guid id, CancellationToken ct = default) =>
        db.Tasks
            .Include(t => t.History.OrderByDescending(h => h.At))
            .FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<List<TaskItem>> ListByCustomerAsync(Guid customerId, CancellationToken ct = default) =>
        db.Tasks.AsNoTracking()
            .Where(x => x.CustomerId == customerId)
            .OrderBy(x => x.Status).ThenByDescending(x => x.Priority).ThenBy(x => x.DueDate)
            .ToListAsync(ct);

    public Task<List<TaskItem>> ListByTeamAsync(Guid teamId, CancellationToken ct = default) =>
        db.Tasks.AsNoTracking()
            .Where(x => x.AssignedTeamId == teamId)
            .OrderBy(x => x.Status).ThenByDescending(x => x.Priority).ThenBy(x => x.DueDate)
            .ToListAsync(ct);

    public async Task<TaskItem> CreateAsync(TaskItem task, CancellationToken ct = default)
    {
        var ctx = await currentUser.GetAsync();
        task.CreatedAt = task.UpdatedAt = DateTime.UtcNow;
        task.CreatedByUserId = ctx.UserId;
        if (task.AssignedTeamId is null)
            task.AssignedUserId ??= ctx.UserId;
        task.DueDate = NormalizeUtc(task.DueDate);
        db.Tasks.Add(task);
        AddHistory(task.Id, "Created", $"Aufgabe angelegt: {task.Title}", ctx);
        await db.SaveChangesAsync(ct);
        return task;
    }

    /// <summary>
    /// Load + patch: only the user-editable fields are touched, audit fields preserved.
    /// Each meaningful change writes one or more <see cref="TaskHistoryEntry"/> rows so
    /// the task detail page shows a per-task audit trail.
    /// </summary>
    public async Task UpdateAsync(TaskItem incoming, CancellationToken ct = default)
    {
        var existing = await db.Tasks.FirstOrDefaultAsync(x => x.Id == incoming.Id, ct);
        if (existing is null) return;

        var ctx = await currentUser.GetAsync();
        var statusBefore = existing.Status;

        if (existing.Title != incoming.Title)
            AddHistory(existing.Id, "Renamed", $"„{existing.Title}\" → „{incoming.Title}\"", ctx);
        if (existing.Status != incoming.Status)
            AddHistory(existing.Id, "Status", $"{Pretty(existing.Status)} → {Pretty(incoming.Status)}", ctx);
        if (existing.Priority != incoming.Priority)
            AddHistory(existing.Id, "Priority", $"{existing.Priority} → {incoming.Priority}", ctx);
        if (existing.DueDate != incoming.DueDate)
            AddHistory(existing.Id, "DueDate", $"Fällig: {Fmt(existing.DueDate)} → {Fmt(incoming.DueDate)}", ctx);
        if (existing.AssignedUserId != incoming.AssignedUserId || existing.AssignedTeamId != incoming.AssignedTeamId)
            AddHistory(existing.Id, "Assignee", "Zuweisung geändert", ctx);
        if (existing.CustomerId != incoming.CustomerId)
            AddHistory(existing.Id, "Customer", "Kontakt-Verknüpfung geändert", ctx);

        existing.Title = incoming.Title;
        existing.Description = incoming.Description;
        existing.Status = incoming.Status;
        existing.Priority = incoming.Priority;
        existing.DueDate = NormalizeUtc(incoming.DueDate);
        existing.AssignedUserId = incoming.AssignedUserId;
        existing.AssignedTeamId = incoming.AssignedTeamId;
        existing.CustomerId = incoming.CustomerId;
        existing.LogCompletionToCustomer = incoming.LogCompletionToCustomer;
        existing.UpdatedAt = DateTime.UtcNow;

        if (statusBefore != TaskStatus.Done && existing.Status == TaskStatus.Done)
            await MaybeLogCompletionToCustomerAsync(existing, ctx, ct);

        await db.SaveChangesAsync(ct);
    }

    public async Task ToggleDoneAsync(Guid id, CancellationToken ct = default)
    {
        var t = await db.Tasks.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (t is null) return;
        var ctx = await currentUser.GetAsync();

        var statusBefore = t.Status;
        t.Status = t.Status == TaskStatus.Done ? TaskStatus.Open : TaskStatus.Done;
        t.UpdatedAt = DateTime.UtcNow;
        AddHistory(t.Id, "Status", $"{Pretty(statusBefore)} → {Pretty(t.Status)}", ctx);

        if (statusBefore != TaskStatus.Done && t.Status == TaskStatus.Done)
            await MaybeLogCompletionToCustomerAsync(t, ctx, ct);

        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var t = await db.Tasks.FindAsync([id], ct);
        if (t is null) return;
        db.Tasks.Remove(t);
        await db.SaveChangesAsync(ct);
    }

    // --- helpers ---------------------------------------------------------

    private void AddHistory(Guid taskId, string action, string details, ActorContext ctx)
    {
        db.TaskHistoryEntries.Add(new TaskHistoryEntry
        {
            TaskId = taskId,
            Action = action,
            Details = details,
            ActorUserId = ctx.UserId ?? string.Empty,
            OnBehalfOfAdminId = ctx.ImpersonatorId
        });
    }

    /// <summary>
    /// When a task transitions to Done and is linked to a contact, mirror the
    /// completion into the contact's history (unless the user has opted out
    /// via <see cref="TaskItem.LogCompletionToCustomer"/>).
    /// </summary>
    private async Task MaybeLogCompletionToCustomerAsync(TaskItem task, ActorContext ctx, CancellationToken ct)
    {
        if (!task.LogCompletionToCustomer) return;
        if (task.CustomerId is not Guid cid) return;

        // Sanity check: customer might have been deleted in the meantime.
        var exists = await db.Customers.AnyAsync(c => c.Id == cid, ct);
        if (!exists) return;

        db.CustomerHistoryEntries.Add(new CustomerHistoryEntry
        {
            CustomerId = cid,
            Action = "Task Completed",
            Details = $"Aufgabe erledigt: {task.Title}",
            ActorUserId = ctx.UserId ?? string.Empty,
            OnBehalfOfAdminId = ctx.ImpersonatorId
        });
    }

    private static DateTime? NormalizeUtc(DateTime? d) =>
        d is null ? null : DateTime.SpecifyKind(d.Value.Date, DateTimeKind.Utc);

    private static string Pretty(TaskStatus s) => s switch
    {
        TaskStatus.Open => "Offen",
        TaskStatus.InProgress => "In Arbeit",
        TaskStatus.Done => "Erledigt",
        TaskStatus.Cancelled => "Abgebrochen",
        _ => s.ToString()
    };

    private static string Fmt(DateTime? d) => d?.ToString("dd.MM.yyyy") ?? "—";
}
