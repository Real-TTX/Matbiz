namespace Matbiz.Web.Modules.Statistics;

/// <summary>
/// Plugin-Schnittstelle für Statistiken pro Modul. Jedes Modul (Kontakte,
/// Aufgaben, Belege, …) implementiert seinen eigenen Provider und registriert
/// ihn als DI-Service. Die Statistik-Seite fragt alle Provider ab und zeigt
/// pro Modul einen eigenen Reiter.
///
/// Provider sind reine Lese-Schicht — keine State-Änderungen.
/// </summary>
public interface IStatisticsProvider
{
    /// <summary>Stabiler Schlüssel — wird als Tab-Routing-Wert benutzt.</summary>
    string ModuleKey { get; }

    /// <summary>Lesbare Modul-Bezeichnung für den Reiter.</summary>
    string DisplayName { get; }

    /// <summary>Bootstrap-Icon-Klasse (bi-*) für den Tab.</summary>
    string Icon { get; }

    /// <summary>Optionale Sortierung der Reiter — niedriger = weiter links.</summary>
    int SortOrder => 100;

    Task<StatisticsModuleResult> GetAsync(CancellationToken ct = default);
}

/// <summary>
/// Aggregat-Ergebnis eines Moduls. Eine Liste von KPI-Kacheln plus
/// optional eine Tabelle (z.B. „Top-5-Kunden nach Umsatz").
/// </summary>
public record StatisticsModuleResult(
    IReadOnlyList<KpiTile> Tiles,
    IReadOnlyList<StatisticsTable>? Tables = null);

/// <summary>Eine KPI-Kachel: großer Wert + Label + optional Trend + Icon.</summary>
public record KpiTile(
    string Label,
    string Value,
    string? Icon = null,
    string? Trend = null,
    string? Color = null);   // "primary" / "success" / "warning" / "danger"

public record StatisticsTable(
    string Title,
    IReadOnlyList<string> Headers,
    IReadOnlyList<IReadOnlyList<string>> Rows);
