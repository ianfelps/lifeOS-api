namespace ServiceLifeOS.Application.Ports;

using ServiceLifeOS.Domain.Entities;

public interface ICurrentUser
{
    string UserId { get; }

    string UserName { get; }

    string TokenId { get; }
}

public interface IUserRepository
{
    Task<AppUser?> GetActiveByUserNameAsync(
        string userName,
        CancellationToken cancellationToken = default);

    Task<AppUser?> GetActiveByIdAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task UpdatePasswordHashAsync(
        string userId,
        string passwordHash,
        DateTime updatedAt,
        CancellationToken cancellationToken = default);
}

public interface IPasswordHasher
{
    string HashPassword(string password);

    bool VerifyPassword(string password, string passwordHash);
}

public interface ITokenService
{
    AccessTokenData CreateAccessToken(string userId, string userName, string displayName);
}

public sealed class AccessTokenData
{
    public string Value { get; init; } = string.Empty;

    public string TokenId { get; init; } = string.Empty;

    public DateTime ExpiresAt { get; init; }
}

public interface IUserPreferenceRepository
{
    Task<UserPreference?> GetByUserIdAsync(
        string userId,
        CancellationToken cancellationToken = default);
}

public interface IUserSessionRepository
{
    Task CreateAsync(UserSession session, CancellationToken cancellationToken = default);

    Task<bool> IsActiveAsync(
        string userId,
        string tokenId,
        DateTime now,
        CancellationToken cancellationToken = default);

    Task TouchAsync(
        string tokenId,
        DateTime now,
        CancellationToken cancellationToken = default);

    Task<int> RevokeOtherActiveSessionsAsync(
        string userId,
        string currentTokenId,
        DateTime now,
        CancellationToken cancellationToken = default);
}

public interface IAuditLogRepository
{
    Task CreateAsync(AuditLog auditLog, CancellationToken cancellationToken = default);

    Task<AuditLogPage> GetPageAsync(
        string userId,
        AuditLogFilter filter,
        CancellationToken cancellationToken = default);
}

public sealed class AuditLogFilter
{
    public int Page { get; init; }

    public int PageSize { get; init; }

    public AuditAction? Action { get; init; }

    public string? ResourceType { get; init; }

    public DateTime? CreatedFrom { get; init; }

    public DateTime? CreatedTo { get; init; }
}

public sealed class AuditLogPage
{
    public IReadOnlyCollection<AuditLog> Items { get; init; } = [];

    public int TotalCount { get; init; }
}
