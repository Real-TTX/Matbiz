using System.ComponentModel.DataAnnotations;

namespace Matbiz.Web.Modules.SystemSettings.Models;

public enum ThemeMode
{
    Light = 0,
    Dark = 1,
    System = 2
}

/// <summary>
/// Singleton-row holding the tenant's branding. There is always exactly one
/// row (<see cref="SingletonId"/>); the service upserts.
/// </summary>
public class BrandingSettings
{
    public static readonly Guid SingletonId = new("00000000-0000-0000-0000-000000000001");

    [Key]
    public Guid Id { get; set; } = SingletonId;

    [MaxLength(100)]
    public string AppName { get; set; } = "Matbiz";

    /// <summary>Primary brand color (buttons, active nav items) as #RRGGBB.</summary>
    [MaxLength(9)]
    public string PrimaryColor { get; set; } = "#1b6ec2";

    /// <summary>Secondary accent color (success / positive states).</summary>
    [MaxLength(9)]
    public string AccentColor1 { get; set; } = "#7c3aed";

    /// <summary>Tertiary accent color (warnings / attention).</summary>
    [MaxLength(9)]
    public string AccentColor2 { get; set; } = "#f59e0b";

    public byte[]? LogoBytes { get; set; }

    [MaxLength(100)]
    public string? LogoContentType { get; set; }

    /// <summary>Bumped on every save; used for cache-busting the /branding/logo URL.</summary>
    public int Version { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
