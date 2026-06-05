using Matbiz.Web.Data;
using Matbiz.Web.Modules.Articles.Models;
using Microsoft.EntityFrameworkCore;

namespace Matbiz.Web.Modules.Articles.Services;

public class NumberRangeService(ApplicationDbContext db)
{
    /// <summary>Default-Nummernkreise — werden beim ersten Zugriff oder via Seeder angelegt.</summary>
    public static readonly NumberRange[] Defaults =
    {
        new() { Key = "Article",    Label = "Artikel",      Prefix = "A",  IncludeYear = false, Separator = "-", Digits = 5 },
        new() { Key = "Offer",      Label = "Angebot",      Prefix = "AN", IncludeYear = true,  Separator = "-", Digits = 4 },
        new() { Key = "Order",      Label = "Auftrag",      Prefix = "AU", IncludeYear = true,  Separator = "-", Digits = 4 },
        new() { Key = "Invoice",    Label = "Rechnung",     Prefix = "RE", IncludeYear = true,  Separator = "-", Digits = 4 },
        new() { Key = "CreditNote", Label = "Gutschrift",   Prefix = "GU", IncludeYear = true,  Separator = "-", Digits = 4 },
        new() { Key = "GoodsReceipt", Label = "Wareneingang", Prefix = "WE", IncludeYear = true, Separator = "-", Digits = 4 },
    };

    public Task<List<NumberRange>> ListAsync(CancellationToken ct = default) =>
        db.NumberRanges.AsNoTracking().OrderBy(x => x.Label).ToListAsync(ct);

    /// <summary>Legt fehlende Default-Nummernkreise an (Startup-Seeder).</summary>
    public async Task EnsureDefaultsAsync(CancellationToken ct = default)
    {
        var existing = await db.NumberRanges.Select(x => x.Key).ToListAsync(ct);
        foreach (var d in Defaults)
        {
            if (existing.Contains(d.Key)) continue;
            db.NumberRanges.Add(new NumberRange
            {
                Key = d.Key, Label = d.Label, Prefix = d.Prefix,
                IncludeYear = d.IncludeYear, Separator = d.Separator, Digits = d.Digits,
                CurrentValue = 0
            });
        }
        await db.SaveChangesAsync(ct);
    }

    public Task<NumberRange?> GetAsync(Guid id, CancellationToken ct = default) =>
        db.NumberRanges.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<NumberRange?> GetByKeyAsync(string key, CancellationToken ct = default) =>
        db.NumberRanges.FirstOrDefaultAsync(x => x.Key == key, ct);

    public async Task UpdateAsync(NumberRange nr, CancellationToken ct = default)
    {
        nr.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Setzt Zählerstand auf einen Wert (Admin-Funktion).</summary>
    public async Task ResetAsync(Guid id, int newValue, int? newYear, CancellationToken ct = default)
    {
        var nr = await db.NumberRanges.FindAsync([id], ct);
        if (nr is null) return;
        nr.CurrentValue = Math.Max(0, newValue);
        nr.CurrentYear = newYear;
        nr.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Holt die nächste Nummer für den Key. Erstellt den Range bei Bedarf
    /// aus den Defaults. Setzt den Zähler bei Jahreswechsel zurück (wenn IncludeYear).</summary>
    public async Task<string> NextAsync(string key, CancellationToken ct = default)
    {
        var nr = await db.NumberRanges.FirstOrDefaultAsync(x => x.Key == key, ct);
        if (nr is null)
        {
            // Aus Defaults seedten
            var template = Defaults.FirstOrDefault(d => d.Key == key);
            nr = template is null
                ? new NumberRange { Key = key, Label = key, Prefix = key.ToUpperInvariant()[..Math.Min(3, key.Length)], IncludeYear = true, Digits = 4 }
                : new NumberRange
                {
                    Key = template.Key, Label = template.Label, Prefix = template.Prefix,
                    IncludeYear = template.IncludeYear, Separator = template.Separator, Digits = template.Digits
                };
            db.NumberRanges.Add(nr);
        }

        var year = DateTime.UtcNow.Year;
        if (nr.IncludeYear && nr.CurrentYear != year)
        {
            nr.CurrentYear = year;
            nr.CurrentValue = 0;
        }
        nr.CurrentValue += 1;
        nr.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return Format(nr);
    }

    /// <summary>Formatiert die nächste Nummer, ohne den Zähler hochzusetzen — Preview im Admin.</summary>
    public string PreviewNext(NumberRange nr)
    {
        var year = DateTime.UtcNow.Year;
        var preview = new NumberRange
        {
            Prefix = nr.Prefix,
            IncludeYear = nr.IncludeYear,
            Separator = nr.Separator,
            Digits = nr.Digits,
            CurrentValue = (nr.IncludeYear && nr.CurrentYear != year ? 0 : nr.CurrentValue) + 1,
            CurrentYear = nr.IncludeYear ? year : null
        };
        return Format(preview);
    }

    public static string Format(NumberRange nr)
    {
        var sep = string.IsNullOrEmpty(nr.Separator) ? "-" : nr.Separator;
        var parts = new List<string>();
        if (!string.IsNullOrEmpty(nr.Prefix)) parts.Add(nr.Prefix);
        if (nr.IncludeYear && nr.CurrentYear is int y) parts.Add(y.ToString());
        var digits = Math.Max(1, nr.Digits);
        parts.Add(nr.CurrentValue.ToString(System.Globalization.CultureInfo.InvariantCulture).PadLeft(digits, '0'));
        return string.Join(sep, parts);
    }
}
