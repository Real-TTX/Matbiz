using System.Text.Json;

namespace Matbiz.Web.Modules.Dashboard;

public enum DashboardWidget
{
    Overdue = 0,
    Today = 1,
    Week = 2,
    TeamUpcoming = 3,
    NewContacts = 4,
    RecentHistory = 5,
    NewCompanies = 6
}

public class WidgetConfig
{
    public DashboardWidget Type { get; set; }
    public bool Enabled { get; set; } = true;
    public int Order { get; set; }
    public int MaxItems { get; set; } = 8;
}

public class DashboardConfig
{
    public List<WidgetConfig> Widgets { get; set; } = new();

    public static DashboardConfig Default() => new()
    {
        Widgets = new()
        {
            new() { Type = DashboardWidget.Overdue,      Order = 0, MaxItems = 8 },
            new() { Type = DashboardWidget.Today,        Order = 1, MaxItems = 8 },
            new() { Type = DashboardWidget.Week,         Order = 2, MaxItems = 8 },
            new() { Type = DashboardWidget.TeamUpcoming, Order = 3, MaxItems = 8 },
            new() { Type = DashboardWidget.NewContacts,  Order = 4, MaxItems = 5 },
            new() { Type = DashboardWidget.NewCompanies, Order = 5, MaxItems = 5 },
            new() { Type = DashboardWidget.RecentHistory,Order = 6, MaxItems = 8 }
        }
    };

    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = false };

    public static DashboardConfig Load(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return Default();
        try
        {
            var cfg = JsonSerializer.Deserialize<DashboardConfig>(json, JsonOpts) ?? Default();
            // Ensure every widget type is present so future-added widgets don't
            // silently disappear for users with an older config blob.
            var defaults = Default();
            foreach (var d in defaults.Widgets)
                if (!cfg.Widgets.Any(w => w.Type == d.Type))
                    cfg.Widgets.Add(d);
            return cfg;
        }
        catch
        {
            return Default();
        }
    }

    public static string Save(DashboardConfig cfg) => JsonSerializer.Serialize(cfg, JsonOpts);
}

public static class DashboardWidgetMeta
{
    public static (string Title, string Icon) For(DashboardWidget w) => w switch
    {
        DashboardWidget.Overdue       => ("Überfällig",            "bi-exclamation-triangle text-danger"),
        DashboardWidget.Today         => ("Heute fällig",          "bi-calendar-day text-primary"),
        DashboardWidget.Week          => ("Diese Woche",           "bi-calendar-week text-primary"),
        DashboardWidget.TeamUpcoming  => ("Team-Aufgaben",         "bi-people-fill"),
        DashboardWidget.NewContacts   => ("Neue Kontakte",         "bi-person-plus"),
        DashboardWidget.NewCompanies  => ("Neue Firmen",           "bi-building-add"),
        DashboardWidget.RecentHistory => ("Letzte Historieneinträge", "bi-clock-history"),
        _ => (w.ToString(), "bi-grid")
    };
}
