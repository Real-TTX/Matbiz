using System.ComponentModel.DataAnnotations;

namespace Matbiz.Web.Modules.Files.Models;

/// <summary>
/// Polymorphic owner of an attached file. The combination of
/// <see cref="AttachedFile.OwnerType"/> + <see cref="AttachedFile.OwnerId"/>
/// identifies what the file is hanging off. No FK constraint — owner
/// integrity is enforced application-side when adding files.
/// </summary>
public enum AttachedFileOwnerType
{
    /// <summary>File library entry on a Customer (contact).</summary>
    CustomerLibrary = 0,
    /// <summary>File library entry on a Company.</summary>
    CompanyLibrary = 1,
    /// <summary>Attachment to a single Customer history entry.</summary>
    CustomerHistory = 2,
    /// <summary>Value backing a "File" type custom field.</summary>
    CustomFieldValue = 3
}

public class AttachedFile
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public AttachedFileOwnerType OwnerType { get; set; }
    public Guid OwnerId { get; set; }

    [Required, MaxLength(255)]
    public string FileName { get; set; } = string.Empty;

    [MaxLength(200)]
    public string ContentType { get; set; } = "application/octet-stream";

    public long SizeBytes { get; set; }

    /// <summary>Raw bytes — Postgres bytea. Fine up to single-digit MB per file;
    /// move to a blob store / filesystem volume if files start growing past that.</summary>
    public byte[] Content { get; set; } = Array.Empty<byte>();

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public string? UploadedByUserId { get; set; }
}
