using Matbiz.Web.Impersonation;
using Microsoft.AspNetCore.Http;

namespace Matbiz.Web.Shared;

/// <summary>
/// Resolves the effectively-acting user (i.e. the impersonation target when
/// active) plus, separately, the real admin id behind the wheel. Service
/// code that writes audit/history rows should record both: the action runs
/// as the target user, but the responsible human is the admin.
/// </summary>
public interface ICurrentUserAccessor
{
    Task<ActorContext> GetAsync();
}

public record ActorContext(string? UserId, string? UserName, string? ImpersonatorId, string? ImpersonatorName)
{
    public bool IsImpersonated => !string.IsNullOrEmpty(ImpersonatorId);
}

public class CurrentUserAccessor(IHttpContextAccessor http) : ICurrentUserAccessor
{
    public Task<ActorContext> GetAsync()
    {
        var u = http.HttpContext?.User;
        if (u?.Identity?.IsAuthenticated != true)
            return Task.FromResult(new ActorContext(null, null, null, null));

        return Task.FromResult(new ActorContext(
            u.UserId(),
            u.UserName(),
            u.ImpersonatorId(),
            u.ImpersonatorName()));
    }
}
