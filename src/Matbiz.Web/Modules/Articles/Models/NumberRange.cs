using System.ComponentModel.DataAnnotations;

namespace Matbiz.Web.Modules.Articles.Models;

/// <summary>
/// Admin-konfigurierbarer Nummernkreis. Format:
///   {Prefix}{Sep}{Year if IncludeYear}{Sep}{Counter padded to Digits}
///
/// Beispiele:
///   Prefix=RE  IncludeYear=true  Sep=-  Digits=4  → „RE-2026-0042"
///   Prefix=A   IncludeYear=false Sep=-  Digits=5  → „A-00042"
///   Prefix=""  IncludeYear=true  Sep=/  Digits=4  → „2026/0042"
///
/// Bei Jahreswechsel wird CurrentValue automatisch zurückgesetzt
/// (wenn IncludeYear=true). Bekannte Keys: „Article", „Offer", „Order",
/// „Invoice", „CreditNote".
/// </summary>
public class NumberRange
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Stabiler technischer Key — wird vom Code referenziert.</summary>
    [Required, MaxLength(50)]
    public string Key { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Label { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Prefix { get; set; }

    public bool IncludeYear { get; set; }

    [MaxLength(5)]
    public string Separator { get; set; } = "-";

    /// <summary>Mindest-Stellen der Zähler-Komponente, z.B. 4 → 0042.</summary>
    public int Digits { get; set; } = 4;

    /// <summary>Letzter vergebener Zählerstand.</summary>
    public int CurrentValue { get; set; }

    /// <summary>Jahr des letzten Zählerstands — relevant bei IncludeYear=true für Auto-Reset.</summary>
    public int? CurrentYear { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
