using System.ComponentModel.DataAnnotations;

namespace Matbiz.Web.Data;

/// <summary>
/// DB-backed authentication ticket store row. The cookie carries only the
/// opaque <see cref="Id"/>; the full ticket payload lives here. Lets us
/// invalidate sessions server-side and inspect who is logged in.
/// </summary>
public class AuthSessionTicket
{
    [Key, MaxLength(64)]
    public string Id { get; set; } = string.Empty;

    public string UserId { get; set; } = string.Empty;

    public byte[] Value { get; set; } = Array.Empty<byte>();

    public DateTime IssuedUtc { get; set; }

    public DateTime? ExpiresUtc { get; set; }

    public DateTime LastAccessedUtc { get; set; } = DateTime.UtcNow;
}
