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

    /// <summary>Separates Dark-Mode-Logo (z.B. helle Variante). Wenn null → wird LogoBytes verwendet.</summary>
    public byte[]? LogoDarkBytes { get; set; }
    [MaxLength(100)]
    public string? LogoDarkContentType { get; set; }

    /// <summary>Bumped on every save; used for cache-busting the /branding/logo URL.</summary>
    public int Version { get; set; }

    /// <summary>Höhe des Logos in der Sidebar (in Pixel, 24–96). Default 40.</summary>
    public int LogoHeightPx { get; set; } = 40;

    /// <summary>Wenn true, wird unter dem Logo zusätzlich der AppName angezeigt.</summary>
    public bool ShowAppNameUnderLogo { get; set; } = true;

    /// <summary>
    /// Logo-Farbinvertierung — „None", „DarkOnly" oder „Always".
    /// Nützlich für schwarz-auf-transparente Logos, die im Dark Mode unsichtbar wären.
    /// Wird per CSS-Filter (invert(1)) umgesetzt — verändert das Bild selbst nicht.
    /// </summary>
    [MaxLength(20)]
    public string LogoInvertMode { get; set; } = "None";

    // === Firma-Stammdaten für Belege (Rechnung, Angebot, …) ===

    /// <summary>Vollständiger Firmenname (oft länger als AppName, z.B. Rechtsform).</summary>
    [MaxLength(200)]
    public string? CompanyLegalName { get; set; }

    [MaxLength(200)] public string? CompanyStreet { get; set; }
    [MaxLength(20)]  public string? CompanyPostalCode { get; set; }
    [MaxLength(100)] public string? CompanyCity { get; set; }
    [MaxLength(100)] public string? CompanyCountry { get; set; }

    [MaxLength(50)]  public string? CompanyEmail { get; set; }
    [MaxLength(50)]  public string? CompanyPhone { get; set; }
    [MaxLength(100)] public string? CompanyWebsite { get; set; }

    /// <summary>Umsatzsteuer-Identifikationsnummer.</summary>
    [MaxLength(30)] public string? VatId { get; set; }

    /// <summary>Steuernummer beim Finanzamt.</summary>
    [MaxLength(30)] public string? TaxNumber { get; set; }

    /// <summary>Geschäftsführer / Inhaber für Impressum / Rechnungsfuß.</summary>
    [MaxLength(200)] public string? ManagingDirector { get; set; }

    /// <summary>Amtsgericht — Pflichtangabe für GmbH/UG/AG nach § 35a GmbHG.</summary>
    [MaxLength(100)] public string? RegistrationCourt { get; set; }

    /// <summary>HRB/HRA-Nummer — Pflichtangabe wie oben.</summary>
    [MaxLength(50)]  public string? RegistrationNumber { get; set; }

    // === Bankverbindung ===
    [MaxLength(100)] public string? BankName { get; set; }
    [MaxLength(34)]  public string? Iban { get; set; }
    [MaxLength(11)]  public string? Bic { get; set; }

    // === PDF-Einstellungen ===

    /// <summary>Zahlungsbedingung als Standard, z.B. „Zahlbar binnen 14 Tagen netto".</summary>
    [MaxLength(500)] public string? DefaultPaymentTerms { get; set; }

    /// <summary>Fußzeilen-Text auf PDF (zusätzlich zur Bankzeile), z.B. AGB-Hinweis.</summary>
    public string? PdfFooterText { get; set; }

    /// <summary>Gewähltes PDF-Layout — Klassisch / Modern / Minimal.</summary>
    [MaxLength(20)]
    public string PdfTemplate { get; set; } = "Classic";

    /// <summary>Aktiver Kontorahmen für DATEV-Export: „SKR03" oder „SKR04".</summary>
    [MaxLength(10)]
    public string ChartOfAccounts { get; set; } = "SKR03";

    /// <summary>Erste freie Debitor-Nummer für Auto-Vergabe (Default 10000).</summary>
    public int NextDebitorNumber { get; set; } = 10000;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
