using Matbiz.Web.Data;
using Matbiz.Web.Modules.CustomFields.Models;
using Microsoft.EntityFrameworkCore;

namespace Matbiz.Web.Modules.CustomFields.Services;

/// <summary>
/// Einzige Anlaufstelle für Custom-Field-Verwaltung über alle Entity-Typen
/// (Kontakte, Artikel, …). Methoden sind durch <see cref="CustomFieldEntityType"/>
/// parametriert, das ersetzt die früheren Customer-/Article-spezifischen Services.
/// </summary>
public class CustomFieldService(ApplicationDbContext db)
{
    // --- Definitions ----------------------------------------------------

    public Task<List<CustomFieldDefinition>> ListAsync(CustomFieldEntityType et, CancellationToken ct = default) =>
        db.CustomFieldDefinitions.AsNoTracking()
            .Where(x => x.EntityType == et)
            .Include(x => x.Section)
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Label)
            .ToListAsync(ct);

    public Task<CustomFieldDefinition?> GetAsync(Guid id, CancellationToken ct = default) =>
        db.CustomFieldDefinitions.Include(x => x.Section).FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<CustomFieldDefinition> CreateAsync(CustomFieldDefinition def, CancellationToken ct = default)
    {
        db.CustomFieldDefinitions.Add(def);
        await db.SaveChangesAsync(ct);
        return def;
    }

    public async Task UpdateAsync(CustomFieldDefinition def, CancellationToken ct = default)
    {
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var d = await db.CustomFieldDefinitions.FindAsync([id], ct);
        if (d is null) return;
        db.CustomFieldDefinitions.Remove(d);
        await db.SaveChangesAsync(ct);
    }

    // --- Sections -------------------------------------------------------

    public Task<List<CustomFieldSection>> ListSectionsAsync(CustomFieldEntityType et, CancellationToken ct = default) =>
        db.CustomFieldSections.AsNoTracking()
            .Where(s => s.EntityType == et)
            .Include(s => s.Fields)
            .OrderBy(s => s.SortOrder).ThenBy(s => s.Name)
            .ToListAsync(ct);

    public Task<CustomFieldSection?> GetSectionAsync(Guid id, CancellationToken ct = default) =>
        db.CustomFieldSections.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<CustomFieldSection> CreateSectionAsync(CustomFieldEntityType et, string name, CancellationToken ct = default)
    {
        var maxOrder = await db.CustomFieldSections.Where(s => s.EntityType == et).MaxAsync(s => (int?)s.SortOrder, ct) ?? 0;
        var s = new CustomFieldSection { EntityType = et, Name = name.Trim(), SortOrder = maxOrder + 1 };
        db.CustomFieldSections.Add(s);
        await db.SaveChangesAsync(ct);
        return s;
    }

    public async Task UpdateSectionAsync(Guid id, string name, CancellationToken ct = default)
    {
        var s = await db.CustomFieldSections.FindAsync([id], ct);
        if (s is null) return;
        s.Name = name.Trim();
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteSectionAsync(Guid id, CancellationToken ct = default)
    {
        var s = await db.CustomFieldSections.FindAsync([id], ct);
        if (s is null) return;
        db.CustomFieldSections.Remove(s);
        await db.SaveChangesAsync(ct);
    }

    // --- Values ---------------------------------------------------------

    public Task<List<CustomFieldValue>> GetValuesAsync(CustomFieldEntityType et, Guid entityId, CancellationToken ct = default) =>
        db.CustomFieldValues.AsNoTracking()
            .Where(v => v.EntityType == et && v.EntityId == entityId)
            .ToListAsync(ct);

    public async Task<Dictionary<Guid, string?>> GetValueMapAsync(CustomFieldEntityType et, Guid entityId, CancellationToken ct = default)
    {
        var vals = await GetValuesAsync(et, entityId, ct);
        return vals.ToDictionary(v => v.FieldDefinitionId, v => v.Value);
    }

    /// <summary>Upsert aller Werte für eine Entity — leere Werte löschen den Eintrag.</summary>
    public async Task SaveValuesAsync(CustomFieldEntityType et, Guid entityId, IDictionary<Guid, string?> values, CancellationToken ct = default)
    {
        var existing = await db.CustomFieldValues
            .Where(v => v.EntityType == et && v.EntityId == entityId)
            .ToListAsync(ct);

        var validDefIds = await db.CustomFieldDefinitions
            .Where(d => d.EntityType == et)
            .Select(d => d.Id).ToListAsync(ct);
        var validSet = validDefIds.ToHashSet();

        foreach (var kv in values)
        {
            if (!validSet.Contains(kv.Key)) continue;
            var v = existing.FirstOrDefault(x => x.FieldDefinitionId == kv.Key);
            if (string.IsNullOrEmpty(kv.Value))
            {
                if (v is not null) db.CustomFieldValues.Remove(v);
            }
            else
            {
                if (v is null)
                    db.CustomFieldValues.Add(new CustomFieldValue
                    {
                        EntityType = et,
                        EntityId = entityId,
                        FieldDefinitionId = kv.Key,
                        Value = kv.Value
                    });
                else v.Value = kv.Value;
            }
        }

        await db.SaveChangesAsync(ct);
    }
}
