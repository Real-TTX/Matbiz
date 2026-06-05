using Matbiz.Web.Data;
using Matbiz.Web.Modules.Documents.Models;
using Microsoft.EntityFrameworkCore;

namespace Matbiz.Web.Modules.Statistics.Providers;

public class DocumentStatisticsProvider(ApplicationDbContext db) : IStatisticsProvider
{
    public string ModuleKey => "documents";
    public string DisplayName => "Belege";
    public string Icon => "bi-file-earmark-text";
    public int SortOrder => 10;

    public async Task<StatisticsModuleResult> GetAsync(CancellationToken ct = default)
    {
        var yearStart = new DateTime(DateTime.UtcNow.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var allDocs = await db.Documents.AsNoTracking()
            .Where(d => d.Status != DocumentStatus.Cancelled)
            .ToListAsync(ct);

        var invoices = allDocs.Where(d => d.Type == DocumentType.Invoice).ToList();
        var offers   = allDocs.Where(d => d.Type == DocumentType.Offer).ToList();

        var revenueYTD = invoices.Where(i => i.DocumentDate >= yearStart && i.Status == DocumentStatus.Paid)
            .Sum(i => i.NetTotal);
        var openClaims = invoices.Where(i => i.Status is DocumentStatus.Sent or DocumentStatus.PartiallyPaid)
            .Sum(i => i.NetTotal);
        var paidThisMonth = invoices.Where(i => i.DocumentDate >= monthStart && i.Status == DocumentStatus.Paid)
            .Sum(i => i.NetTotal);
        var openOffers = offers.Count(o => o.Status is DocumentStatus.Sent or DocumentStatus.Draft);

        var tiles = new List<KpiTile>
        {
            new("Umsatz YTD (bezahlt)", revenueYTD.ToString("N2") + " €", "bi-graph-up", Color: "success"),
            new("Offene Forderungen",   openClaims.ToString("N2") + " €", "bi-exclamation-circle", Color: openClaims > 0 ? "warning" : null),
            new("Umsatz diesen Monat",  paidThisMonth.ToString("N2") + " €", "bi-calendar-month"),
            new("Offene Angebote",      openOffers.ToString(),               "bi-file-earmark-plus"),
        };

        // Top-5-Kunden nach Umsatz (alle bezahlten Rechnungen)
        var topCustomers = invoices
            .Where(i => i.Status == DocumentStatus.Paid && !string.IsNullOrEmpty(i.CustomerNameSnapshot))
            .GroupBy(i => i.CustomerNameSnapshot!)
            .Select(g => new { Name = g.Key, Net = g.Sum(x => x.NetTotal), Count = g.Count() })
            .OrderByDescending(x => x.Net)
            .Take(5)
            .ToList();

        var tables = new List<StatisticsTable>();
        if (topCustomers.Count > 0)
        {
            tables.Add(new StatisticsTable(
                "Top 5 Kunden (Umsatz, bezahlt)",
                new[] { "Kunde", "Umsatz", "Rechnungen" },
                topCustomers.Select(c => (IReadOnlyList<string>)new[]
                {
                    c.Name, c.Net.ToString("N2") + " €", c.Count.ToString()
                }).ToList()));
        }

        return new(tiles, tables);
    }
}
