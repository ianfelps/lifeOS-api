using System.Security.Claims;
using ServiceLifeOS.Application.Ports;

namespace ServiceLifeOS.Api.Adapters;

public sealed class CurrentUserService : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string UserId =>
        ResolveUserId() ?? throw new UnauthorizedAccessException("User is not authenticated.");

    public string UserName =>
        _httpContextAccessor.HttpContext?.User.FindFirstValue("username") ??
        _httpContextAccessor.HttpContext?.User.FindFirstValue("name") ??
        string.Empty;

    public string TokenId =>
        _httpContextAccessor.HttpContext?.User.FindFirstValue("jti")?.Trim() ??
        throw new UnauthorizedAccessException("Session token was not found.");

    private string? ResolveUserId()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        var nameIdentifier = user?.FindFirstValue(ClaimTypes.NameIdentifier)?.Trim();
        var subject = user?.FindFirstValue("sub")?.Trim();

        if (!string.IsNullOrWhiteSpace(nameIdentifier) &&
            !string.IsNullOrWhiteSpace(subject) &&
            !string.Equals(nameIdentifier, subject, StringComparison.Ordinal))
        {
            return null;
        }

        return string.IsNullOrWhiteSpace(subject)
            ? nameIdentifier
            : subject;
    }
}
