using Matbiz.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace Matbiz.Web.Modules.Statistics.Providers;

public class CustomerStatisticsProvider(ApplicationDbContext db) : IStatisticsProvider
{
    public string ModuleKey => "customers";
    public string DisplayName => "Kontakte";
    public string Icon => "bi-person-vcard";
    public int SortOrder => 20;

    public async Task<StatisticsModuleResult> GetAsync(CancellationToken ct = default)
    {
        var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var yearStart  = new DateTime(DateTime.UtcNow.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var weekAgo    = DateTime.UtcNow.AddDays(-7);

        var total       = await db.Customers.CountAsync(ct);
        var newThisMonth= await db.Customers.CountAsync(c => c.CreatedAt >= monthStart, ct);
        var newThisYear = await db.Customers.CountAsync(c => c.CreatedAt >= yearStart, ct);
        var companies   = await db.Companies.CountAsync(ct);
        var entries7    = await db.CustomerHistoryEntries.CountAsync(h => h.At >= weekAgo, ct);

        var tiles = new List<KpiTile>
        {
            new("Kontakte gesamt",     total.ToString(),        "bi-people"),
            new("Firmen",              companies.ToString(),    "bi-building"),
            new("Neu diesen Monat",    newThisMonth.ToString(), "bi-person-plus",  Color: "success"),
            new("Neu dieses Jahr",     newThisYear.ToString(),  "bi-calendar-check"),
            new("Historien-Einträge (7 Tage)", entries7.ToString(), "bi-clock-history"),
        };

        // === Historien-Aktivität pro Team (7 Tage) ===
        // Plan: alle Einträge der letzten 7 Tage holen, mit Team-Membership des
        // ActorUserId joinen, gruppieren. Aggregat zusätzlich nach Action.
        var rawEntries = await db.CustomerHistoryEntries.AsNoTracking()
            .Where(h => h.At >= weekAgo && h.ActorUserId != "")
            .Select(h => new { h.ActorUserId, h.Action })
            .ToListAsync(ct);

        if (rawEntries.Count == 0)
            return new(tiles);

        var actorIds = rawEntries.Select(e => e.ActorUserId).Distinct().ToList();
        var memberships = await db.TeamMembers.AsNoTracking()
            .Where(m => actorIds.Contains(m.UserId))
            .Include(m => m.Team).ThenInclude(t => t!.Department)
            .Select(m => new { m.UserId, m.Team })
            .ToListAsync(ct);

        // Ein User kann in mehreren Teams sein → Eintrag zählt für jedes seiner Teams.
        var perTeam = (
            from e in rawEntries
            join m in memberships on e.ActorUserId equals m.UserId
            group e by new { m.Team.Id, m.Team.Name, DeptName = m.Team.Department != null ? m.Team.Department.Name : "—" }
                into g
            select new
            {
                Team = g.Key.Name,
                Department = g.Key.DeptName,
                Notes = g.Count(x => x.Action == "Note"),
                Updates = g.Count(x => x.Action == "Updated"),
                Other = g.Count(x => x.Action != "Note" && x.Action != "Updated" && x.Action != "Created"),
                Total = g.Count()
            }
        ).OrderByDescending(x => x.Total).ToList();

        var tables = new List<StatisticsTable>();
        if (perTeam.Count > 0)
        {
            tables.Add(new StatisticsTable(
                "Kontakt-Historie pro Team (letzte 7 Tage)",
                new[] { "Team", "Abteilung", "Notizen", "Änderungen", "Sonstige", "Gesamt" },
                perTeam.Select(r => (IReadOnlyList<string>)new[]
                {
                    r.Team, r.Department,
                    r.Notes.ToString(), r.Updates.ToString(), r.Other.ToString(),
                    r.Total.ToString()
                }).ToList()));
        }

        // === Abteilungs-Aggregat ===
        var perDept = perTeam
            .Where(x => x.Department != "—")
            .GroupBy(x => x.Department)
            .Select(g => new
            {
                Department = g.Key,
                Total = g.Sum(x => x.Total),
                Notes = g.Sum(x => x.Notes),
                Updates = g.Sum(x => x.Updates)
            })
            .OrderByDescending(x => x.Total)
            .ToList();

        if (perDept.Count > 0)
        {
            tables.Add(new StatisticsTable(
                "Kontakt-Historie pro Abteilung (letzte 7 Tage)",
                new[] { "Abteilung", "Notizen", "Änderungen", "Gesamt" },
                perDept.Select(r => (IReadOnlyList<string>)new[]
                {
                    r.Department, r.Notes.ToString(), r.Updates.ToString(), r.Total.ToString()
                }).ToList()));
        }

        // === Top-Aktivität pro Aktion ===
        var perAction = rawEntries.GroupBy(e => e.Action)
            .Select(g => new { Action = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(6)
            .ToList();

        if (perAction.Count > 0)
        {
            tables.Add(new StatisticsTable(
                "Top-Aktivitäten (letzte 7 Tage)",
                new[] { "Aktion", "Anzahl" },
                perAction.Select(r => (IReadOnlyList<string>)new[]
                {
                    LabelAction(r.Action), r.Count.ToString()
                }).ToList()));
        }

        return new(tiles, tables);
    }

    private static string LabelAction(string action) => action switch
    {
        "Note"    => "Notiz",
        "Created" => "Anlage",
        "Updated" => "Änderung",
        _         => action
    };
}
