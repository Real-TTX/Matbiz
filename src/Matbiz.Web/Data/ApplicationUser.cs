using Matbiz.Web.Modules.SystemSettings.Models;
using Microsoft.AspNetCore.Identity;

namespace Matbiz.Web.Data;

public class ApplicationUser : IdentityUser
{
    public string? DisplayName { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Per-user UI theme preference. Defaults to following the OS.</summary>
    public ThemeMode Theme { get; set; } = ThemeMode.System;

    /// <summary>JSON-serialized <see cref="Matbiz.Web.Modules.Dashboard.DashboardConfig"/>.
    /// Null means the user has never customized — we use the default layout.</summary>
    public string? DashboardConfigJson { get; set; }

    /// <summary>JSON-serialized per-list column visibility preferences.
    /// See <see cref="Matbiz.Web.Shared.ListPreferences"/>.</summary>
    public string? ListPreferencesJson { get; set; }
}

public static class Roles
{
    public const string Admin = "Admin";
    public const string User = "User";

    public static readonly string[] All = [Admin, User];
}
