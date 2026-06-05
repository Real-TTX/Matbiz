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
    NewCompanies = 6,
    /// <summary>Custom-Widget: zeigt die ersten N Mitglieder einer Gruppe.</summary>
    CustomerGroup = 7
}

public class WidgetConfig
{
    /// <summary>Pro-Widget-Instanz eindeutig — erlaubt mehrere Widgets gleichen Typs
    /// (z.B. mehrere Gruppen-Widgets, jedes mit anderer GroupId).</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    public DashboardWidget Type { get; set; }
    public bool Enabled { get; set; } = true;
    public int Order { get; set; }
    public int MaxItems { get; set; } = 8;

    // === Custom-Widget Payload ===

    /// <summary>Für Type=CustomerGroup: welche Gruppe wird angezeigt.</summary>
    public Guid? GroupId { get; set; }

    /// <summary>Optional: vom User vergebener Titel statt Gruppen-Name.</summary>
    public string? CustomTitle { get; set; }
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
            // Migration: für alte Configs ohne Id → neue Guid vergeben (sonst kollidieren mehrere
            // Instanzen des selben Typs später).
            foreach (var w in cfg.Widgets)
                if (w.Id == Guid.Empty) w.Id = Guid.NewGuid();
            // Standard-Widgets ergänzen, falls sie in der gespeicherten Config fehlen.
            // CustomerGroup-Typ überspringen — ist nutzer-erstellt, nicht Default.
            var defaults = Default();
            foreach (var d in defaults.Widgets)
            {
                if (d.Type == DashboardWidget.CustomerGroup) continue;
                if (!cfg.Widgets.Any(w => w.Type == d.Type))
                    cfg.Widgets.Add(d);
            }
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
        DashboardWidget.CustomerGroup => ("Gruppe",                   "bi-collection"),
        _ => (w.ToString(), "bi-grid")
    };
}
