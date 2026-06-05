using Matbiz.Web.Data;
using Matbiz.Web.Modules.Modules.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Matbiz.Web.Modules.Modules.Services;

/// <summary>
/// Zentrale Registry aller Module + Lese-/Schreibzugriff auf den
/// Enabled-Status. Status wird im IMemoryCache gehalten (60s) — vermeidet
/// DB-Roundtrip auf jedem Request.
/// </summary>
public class ModuleRegistry(ApplicationDbContext db, IMemoryCache cache)
{
    private const string CacheKey = "module-states";

    /// <summary>Alle bekannten Module — fest verdrahtet, nicht aus DB.</summary>
    public static readonly IReadOnlyList<ModuleDefinition> AllModules = new[]
    {
        // === Basis (nicht deaktivierbar) ===
        new ModuleDefinition("Users",      "Benutzerverwaltung", "Login, Rollen, Teams, Abteilungen — die App braucht Benutzer.", "bi-people", IsCore: true, SortOrder: 1),
        new ModuleDefinition("Customers",  "Kontakte & Firmen",  "Adressbuch, Tags, Gruppen, Custom-Fields — Kern jedes CRM/ERP.", "bi-person-vcard", IsCore: true, SortOrder: 2),
        new ModuleDefinition("System",     "System & Branding",  "App-Name, Farben, Logo, Firma-Stammdaten, PDF-Layout.",          "bi-gear", IsCore: true, SortOrder: 3),

        // === Optional ===
        new ModuleDefinition("Tasks",       "Aufgaben",          "Aufgaben mit Status, Prio, Team, Historie.",                    "bi-check2-square", IsCore: false, SortOrder: 10),
        new ModuleDefinition("Wiki",        "Wiki",              "Interne Wissensbasis, Markdown, Sichtbarkeit pro Team.",        "bi-book", IsCore: false, SortOrder: 20),
        new ModuleDefinition("Articles",    "Artikel",           "Produkte und Leistungen als Stammdaten.",                       "bi-box-seam", IsCore: false, SortOrder: 30),
        new ModuleDefinition("Documents",   "Auftragsbearbeitung","Angebote, Aufträge, Rechnungen, Gutschriften + PDF + XRechnung.","bi-file-earmark-text", IsCore: false, SortOrder: 40),
        new ModuleDefinition("Statistics",  "Statistik",         "KPI-Dashboard pro Modul.",                                       "bi-bar-chart", IsCore: false, SortOrder: 50),
        new ModuleDefinition("CustomMenu",  "Custom Menü",       "Eigene Sidebar-Links + iframe-Tabs auf Kontakt/Firma.",         "bi-list-ul", IsCore: false, SortOrder: 60),
        new ModuleDefinition("Warehouse",   "Lager",             "Lager-Standorte, Bestände, Wareneingänge + Bewegungs-Log.",     "bi-box-seam-fill", IsCore: false, SortOrder: 35),
    };

    public bool IsEnabled(string key)
    {
        // Core ist immer an, egal was in DB steht
        var def = AllModules.FirstOrDefault(m => m.Key == key);
        if (def is null) return false;
        if (def.IsCore) return true;

        var states = GetStates();
        return states.GetValueOrDefault(key, true);  // Default: aktiviert wenn kein Eintrag
    }

    public async Task SetEnabledAsync(string key, bool enabled, CancellationToken ct = default)
    {
        var def = AllModules.FirstOrDefault(m => m.Key == key);
        if (def is null || def.IsCore) return;  // Core ignorieren

        var row = await db.ModuleSettings.FindAsync(new object[] { key }, ct);
        if (row is null)
        {
            row = new ModuleSetting { Key = key };
            db.ModuleSettings.Add(row);
        }
        row.IsEnabled = enabled;
        row.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        cache.Remove(CacheKey);
    }

    private Dictionary<string, bool> GetStates()
    {
        return cache.GetOrCreate(CacheKey, e =>
        {
            e.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60);
            return db.ModuleSettings.AsNoTracking()
                .ToDictionary(m => m.Key, m => m.IsEnabled);
        })!;
    }
}
