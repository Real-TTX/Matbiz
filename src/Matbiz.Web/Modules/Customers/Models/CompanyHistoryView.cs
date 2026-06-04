namespace Matbiz.Web.Modules.Customers.Models;

/// <summary>
/// Merged view-model row for the company history table. Either an own
/// <see cref="CompanyHistoryEntry"/> (SourceLabel "Firma") or one borrowed
/// from a linked contact (SourceContactId set, SourceLabel like "Kontakt: …").
/// </summary>
public record CompanyHistoryView(
    Guid Id,
    DateTime At,
    string Action,
    string? Details,
    string ActorUserId,
    string? OnBehalfOfAdminId,
    string SourceLabel,
    Guid? SourceContactId);
