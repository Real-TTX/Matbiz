using System.ComponentModel.DataAnnotations;
using Matbiz.Web.Modules.Customers.Models;

namespace Matbiz.Web.Modules.Documents.Models;

public enum DocumentType
{
    Offer = 0,        // Angebot
    Order = 1,        // Auftragsbestätigung
    Invoice = 2,      // Rechnung
    CreditNote = 3    // Gutschrift
}

public enum DocumentStatus
{
    Draft = 0,           // Entwurf — bearbeitbar
    Sent = 1,            // Versendet
    Accepted = 2,        // Angenommen (Offer/Order)
    PartiallyPaid = 3,   // Teilweise bezahlt (Invoice)
    Paid = 4,            // Bezahlt (Invoice)
    Cancelled = 5        // Storniert
}

/// <summary>
/// Ein Beleg — Angebot, Auftrag, Rechnung oder Gutschrift. Der Type-Diskriminator
/// teilt die Tabelle, statt 4 fast-identische zu haben.
///
/// Kontakt-/Firma-Adressen werden als <b>Snapshot</b> gespeichert (CustomerNameSnapshot,
/// CustomerAddressSnapshot) — der Beleg darf sich nicht ändern wenn der Kontakt
/// später umzieht oder umfirmiert.
///
/// Vorgangskette: <see cref="SourceDocumentId"/> verlinkt z.B. Rechnung → Angebot.
/// </summary>
public class Document
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public DocumentType Type { get; set; }

    [Required, MaxLength(50)]
    public string Number { get; set; } = string.Empty;

    public DocumentStatus Status { get; set; } = DocumentStatus.Draft;

    public DateTime DocumentDate { get; set; } = DateTime.UtcNow;

    /// <summary>Bei Rechnungen: Zahlungsziel. Bei Angeboten: Gültigkeitsdatum.</summary>
    public DateTime? DueDate { get; set; }

    /// <summary>Leistungs-/Lieferdatum (ZUGFeRD BT-72) — ≠ Belegdatum.
    /// Pflicht-Felder bei Rechnungen für DE-Steuerrecht.</summary>
    public DateTime? ServiceDate { get; set; }

    /// <summary>Währung als ISO-4217 (EUR / USD / CHF). ZUGFeRD BT-5.</summary>
    [MaxLength(3)]
    public string CurrencyCode { get; set; } = "EUR";

    /// <summary>Bestellnummer beim Käufer (ZUGFeRD BT-13).</summary>
    [MaxLength(100)]
    public string? BuyerOrderNumber { get; set; }

    /// <summary>Vertragsnummer (ZUGFeRD BT-12).</summary>
    [MaxLength(100)]
    public string? ContractNumber { get; set; }

    /// <summary>Leitweg-ID des Käufers (ZUGFeRD BT-10) — Pflicht für B2G-XRechnung.</summary>
    [MaxLength(50)]
    public string? BuyerReference { get; set; }

    /// <summary>USt-ID des Käufers als Snapshot zum Belegdatum (ZUGFeRD BT-48).</summary>
    [MaxLength(30)]
    public string? BuyerVatIdSnapshot { get; set; }

    // === Empfänger ===

    public Guid? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public Guid? CompanyId { get; set; }
    public Company? Company { get; set; }

    [MaxLength(200)] public string? CustomerNameSnapshot { get; set; }
    [MaxLength(500)] public string? CustomerAddressSnapshot { get; set; }
    [MaxLength(50)]  public string? CustomerEmailSnapshot { get; set; }

    // === Vorgangskette ===

    public Guid? SourceDocumentId { get; set; }
    public Document? SourceDocument { get; set; }

    // === Texte ===

    /// <summary>Frei-Text oberhalb der Position-Tabelle, z.B. „Vielen Dank für Ihre Anfrage…".</summary>
    public string? HeaderText { get; set; }

    /// <summary>Frei-Text unter der Position-Tabelle.</summary>
    public string? FooterText { get; set; }

    /// <summary>Zahlungsbedingung (überschreibt Default aus Branding).</summary>
    [MaxLength(500)] public string? PaymentTerms { get; set; }

    // === Summen (cached aus Positionen) ===

    public decimal NetTotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal GrossTotal { get; set; }

    // === Audit ===

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    [Required] public string CreatedByUserId { get; set; } = string.Empty;

    public List<DocumentPosition> Positions { get; set; } = new();
}
