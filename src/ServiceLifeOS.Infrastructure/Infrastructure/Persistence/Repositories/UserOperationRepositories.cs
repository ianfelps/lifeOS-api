using Microsoft.EntityFrameworkCore;
using ServiceLifeOS.Application.Ports;
using ServiceLifeOS.Domain.Entities;

namespace ServiceLifeOS.Infrastructure.Persistence.Repositories;

public sealed class UserPreferenceRepository : IUserPreferenceRepository
{
    private readonly AppDbContext _db;

    public UserPreferenceRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<UserPreference?> GetByUserIdAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return _db.UserPreferences.FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
    }
}

public sealed class UserSessionRepository : IUserSessionRepository
{
    private readonly AppDbContext _db;

    public UserSessionRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task CreateAsync(UserSession session, CancellationToken cancellationToken = default)
    {
        _db.UserSessions.Add(session);
        return Task.CompletedTask;
    }

    public Task<bool> IsActiveAsync(
        string userId,
        string tokenId,
        DateTime now,
        CancellationToken cancellationToken = default)
    {
        return _db.UserSessions.AsNoTracking().AnyAsync(
            x => x.UserId == userId &&
                x.TokenId == tokenId &&
                x.RevokedAt == null &&
                x.ExpiresAt > now,
            cancellationToken);
    }

    public async Task TouchAsync(
        string tokenId,
        DateTime now,
        CancellationToken cancellationToken = default)
    {
        await _db.UserSessions
            .Where(x => x.TokenId == tokenId && x.RevokedAt == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(x => x.LastUsedAt, now),
                cancellationToken);
    }

    public Task<int> RevokeOtherActiveSessionsAsync(
        string userId,
        string currentTokenId,
        DateTime now,
        CancellationToken cancellationToken = default)
    {
        return _db.UserSessions
            .Where(x => x.UserId == userId &&
                x.TokenId != currentTokenId &&
                x.RevokedAt == null &&
                x.ExpiresAt > now)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(x => x.RevokedAt, now),
                cancellationToken);
    }
}

public sealed class AuditLogRepository : IAuditLogRepository
{
    private readonly AppDbContext _db;

    public AuditLogRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task CreateAsync(AuditLog auditLog, CancellationToken cancellationToken = default)
    {
        _db.AuditLogs.Add(auditLog);
        return Task.CompletedTask;
    }

    public async Task<AuditLogPage> GetPageAsync(
        string userId,
        AuditLogFilter filter,
        CancellationToken cancellationToken = default)
    {
        var query = _db.AuditLogs
            .AsNoTracking()
            .Where(x => x.UserId == userId);

        if (filter.Action.HasValue)
        {
            query = query.Where(x => x.Action == filter.Action.Value);
        }
        if (!string.IsNullOrWhiteSpace(filter.ResourceType))
        {
            query = query.Where(x => x.ResourceType == filter.ResourceType);
        }
        if (filter.CreatedFrom.HasValue)
        {
            query = query.Where(x => x.CreatedAt >= filter.CreatedFrom.Value);
        }
        if (filter.CreatedTo.HasValue)
        {
            query = query.Where(x => x.CreatedAt <= filter.CreatedTo.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToArrayAsync(cancellationToken);
        return new() { Items = items, TotalCount = totalCount };
    }
}
