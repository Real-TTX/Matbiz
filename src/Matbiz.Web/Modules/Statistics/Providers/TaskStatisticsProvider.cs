using Matbiz.Web.Data;
using Matbiz.Web.Modules.Tasks.Models;
using Microsoft.EntityFrameworkCore;
using TaskStatus = Matbiz.Web.Modules.Tasks.Models.TaskStatus;

namespace Matbiz.Web.Modules.Statistics.Providers;

public class TaskStatisticsProvider(ApplicationDbContext db) : IStatisticsProvider
{
    public string ModuleKey => "tasks";
    public string DisplayName => "Aufgaben";
    public string Icon => "bi-check2-square";
    public int SortOrder => 30;

    public async Task<StatisticsModuleResult> GetAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var weekAgo = now.AddDays(-7);

        var open = await db.Tasks.CountAsync(t => t.Status == TaskStatus.Open || t.Status == TaskStatus.InProgress, ct);
        var overdue = await db.Tasks.CountAsync(t =>
            (t.Status == TaskStatus.Open || t.Status == TaskStatus.InProgress)
            && t.DueDate != null && t.DueDate < now, ct);
        var doneLastWeek = await db.Tasks.CountAsync(t => t.Status == TaskStatus.Done && t.UpdatedAt >= weekAgo, ct);
        var urgent = await db.Tasks.CountAsync(t =>
            t.Priority == TaskPriority.Urgent &&
            (t.Status == TaskStatus.Open || t.Status == TaskStatus.InProgress), ct);

        var tiles = new List<KpiTile>
        {
            new("Offene Aufgaben",   open.ToString(),         "bi-list-task"),
            new("Überfällig",        overdue.ToString(),      "bi-exclamation-triangle", Color: overdue > 0 ? "danger" : null),
            new("Dringend offen",    urgent.ToString(),       "bi-fire",                 Color: urgent > 0 ? "warning" : null),
            new("Erledigt (7 Tage)", doneLastWeek.ToString(), "bi-check-circle",         Color: "success")
        };

        // === Team-Aktivität (7 Tage) ===
        var teams = await db.Teams.AsNoTracking()
            .Include(t => t.Department)
            .OrderBy(t => t.Name)
            .ToListAsync(ct);

        var tasksRaw = await db.Tasks.AsNoTracking()
            .Where(t => t.AssignedTeamId != null
                       && (t.CreatedAt >= weekAgo || (t.Status == TaskStatus.Done && t.UpdatedAt >= weekAgo)))
            .Select(t => new { t.AssignedTeamId, t.Status, t.CreatedAt, t.UpdatedAt })
            .ToListAsync(ct);

        var openByTeam = await db.Tasks.AsNoTracking()
            .Where(t => t.AssignedTeamId != null
                       && (t.Status == TaskStatus.Open || t.Status == TaskStatus.InProgress))
            .GroupBy(t => t.AssignedTeamId!.Value)
            .Select(g => new { TeamId = g.Key, Open = g.Count() })
            .ToDictionaryAsync(x => x.TeamId, x => x.Open, ct);

        var teamRows = teams.Select(t =>
        {
            var received = tasksRaw.Count(x => x.AssignedTeamId == t.Id && x.CreatedAt >= weekAgo);
            var done     = tasksRaw.Count(x => x.AssignedTeamId == t.Id && x.Status == TaskStatus.Done && x.UpdatedAt >= weekAgo);
            var currentlyOpen = openByTeam.GetValueOrDefault(t.Id, 0);
            return new
            {
                Team = t,
                Received = received,
                Done = done,
                Open = currentlyOpen
            };
        })
        .Where(r => r.Received > 0 || r.Done > 0 || r.Open > 0)
        .OrderByDescending(r => r.Done)
        .ThenByDescending(r => r.Received)
        .ToList();

        var tables = new List<StatisticsTable>();
        if (teamRows.Count > 0)
        {
            tables.Add(new StatisticsTable(
                "Team-Aktivität (letzte 7 Tage)",
                new[] { "Team", "Abteilung", "Erhalten", "Erledigt", "Aktuell offen" },
                teamRows.Select(r => (IReadOnlyList<string>)new[]
                {
                    r.Team.Name,
                    r.Team.Department?.Name ?? "—",
                    r.Received.ToString(),
                    r.Done.ToString(),
                    r.Open.ToString()
                }).ToList()));
        }

        // === Abteilungs-Aggregat (7 Tage) ===
        var deptRows = teamRows
            .Where(r => r.Team.Department is not null)
            .GroupBy(r => r.Team.Department!)
            .Select(g => new
            {
                Department = g.Key,
                Received = g.Sum(x => x.Received),
                Done = g.Sum(x => x.Done),
                Open = g.Sum(x => x.Open)
            })
            .OrderByDescending(r => r.Done)
            .ToList();

        if (deptRows.Count > 0)
        {
            tables.Add(new StatisticsTable(
                "Abteilungs-Aktivität (letzte 7 Tage)",
                new[] { "Abteilung", "Erhalten", "Erledigt", "Aktuell offen" },
                deptRows.Select(r => (IReadOnlyList<string>)new[]
                {
                    r.Department.Name,
                    r.Received.ToString(),
                    r.Done.ToString(),
                    r.Open.ToString()
                }).ToList()));
        }

        return new(tiles, tables);
    }
}
