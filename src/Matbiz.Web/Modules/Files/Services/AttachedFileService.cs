using Matbiz.Web.Data;
using Matbiz.Web.Modules.Files.Models;
using Matbiz.Web.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Matbiz.Web.Modules.Files.Services;

public class AttachedFileService(ApplicationDbContext db, ICurrentUserAccessor currentUser)
{
    public const long MaxBytes = 25 * 1024 * 1024; // 25 MB per file

    public async Task<AttachedFile?> UploadAsync(AttachedFileOwnerType ownerType, Guid ownerId, IFormFile file, CancellationToken ct = default)
    {
        if (file is null || file.Length == 0) return null;
        if (file.Length > MaxBytes) throw new InvalidOperationException("Datei zu groß.");

        await using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);

        var ctx = await currentUser.GetAsync();
        var entity = new AttachedFile
        {
            OwnerType = ownerType,
            OwnerId = ownerId,
            FileName = Path.GetFileName(file.FileName), // strip any path the browser may include
            ContentType = string.IsNullOrEmpty(file.ContentType) ? "application/octet-stream" : file.ContentType,
            SizeBytes = file.Length,
            Content = ms.ToArray(),
            UploadedByUserId = ctx.UserId
        };
        db.AttachedFiles.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity;
    }

    public Task<AttachedFile?> GetAsync(Guid id, CancellationToken ct = default) =>
        db.AttachedFiles.FirstOrDefaultAsync(x => x.Id == id, ct);

    /// <summary>Lightweight metadata for list rendering (no Content blob).</summary>
    public Task<List<AttachedFile>> ListForOwnerAsync(AttachedFileOwnerType ownerType, Guid ownerId, CancellationToken ct = default) =>
        db.AttachedFiles.AsNoTracking()
            .Where(f => f.OwnerType == ownerType && f.OwnerId == ownerId)
            .Select(f => new AttachedFile
            {
                Id = f.Id,
                OwnerType = f.OwnerType,
                OwnerId = f.OwnerId,
                FileName = f.FileName,
                ContentType = f.ContentType,
                SizeBytes = f.SizeBytes,
                UploadedAt = f.UploadedAt,
                UploadedByUserId = f.UploadedByUserId
                // Content intentionally omitted
            })
            .OrderByDescending(f => f.UploadedAt)
            .ToListAsync(ct);

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var f = await db.AttachedFiles.FindAsync([id], ct);
        if (f is null) return;
        db.AttachedFiles.Remove(f);
        await db.SaveChangesAsync(ct);
    }
}
