using Microsoft.EntityFrameworkCore;
using ServiceLifeOS.Application.Ports;
using ServiceLifeOS.Domain.Entities;

namespace ServiceLifeOS.Infrastructure.Persistence.Repositories;

public sealed class HabitRepository : IHabitRepository
{
    private readonly AppDbContext _db;

    public HabitRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<Habit?> GetHabitAsync(
        string userId,
        Guid habitId,
        CancellationToken cancellationToken = default)
    {
        return _db.Habits.FirstOrDefaultAsync(
            x => x.UserId == userId && x.Id == habitId,
            cancellationToken);
    }

    public async Task<IReadOnlyCollection<Habit>> GetHabitsAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await _db.Habits
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .ToArrayAsync(cancellationToken);
    }

    public Task<HabitSchedule?> GetScheduleAsync(
        Guid habitId,
        CancellationToken cancellationToken = default)
    {
        return _db.HabitSchedules.FirstOrDefaultAsync(
            x => x.HabitId == habitId,
            cancellationToken);
    }

    public async Task<IReadOnlyCollection<HabitScheduleWeekday>> GetWeekdaysAsync(
        Guid scheduleId,
        CancellationToken cancellationToken = default)
    {
        return await _db.HabitScheduleWeekdays
            .AsNoTracking()
            .Where(x => x.HabitScheduleId == scheduleId)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<HabitCompletion>> GetCompletionsAsync(
        string userId,
        Guid habitId,
        CancellationToken cancellationToken = default)
    {
        return await _db.HabitCompletions
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.HabitId == habitId)
            .ToArrayAsync(cancellationToken);
    }

    public Task<HabitCompletion?> GetCompletionAsync(
        string userId,
        Guid habitId,
        Guid completionId,
        CancellationToken cancellationToken = default)
    {
        return _db.HabitCompletions.FirstOrDefaultAsync(
            x => x.UserId == userId &&
                x.HabitId == habitId &&
                x.Id == completionId &&
                x.DeletedAt == null,
            cancellationToken);
    }

    public Task<XpEventRule?> GetXpRuleAsync(
        string userId,
        XpEventType eventType,
        CancellationToken cancellationToken = default)
    {
        return _db.XpEventRules
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.UserId == userId && x.EventType == eventType,
                cancellationToken);
    }

    public async Task<IReadOnlyCollection<XpLedgerEntry>> GetXpEntriesForSourceAsync(
        string userId,
        string sourceType,
        Guid sourceId,
        CancellationToken cancellationToken = default)
    {
        return await _db.XpLedgerEntries
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.SourceType == sourceType && x.SourceId == sourceId)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<XpLedgerEntry>> GetXpEntriesAsync(
        string userId,
        string sourceType,
        CancellationToken cancellationToken = default)
    {
        return await _db.XpLedgerEntries
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.SourceType == sourceType)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Badge>> GetBadgesAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await _db.Badges
            .AsNoTracking()
            .Where(x => x.UserId == userId && !x.Archived)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<BadgeCriterion>> GetBadgeCriteriaAsync(
        IReadOnlyCollection<Guid> badgeIds,
        CancellationToken cancellationToken = default)
    {
        return await _db.BadgeCriteria
            .AsNoTracking()
            .Where(x => badgeIds.Contains(x.BadgeId))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<UserBadge>> GetUserBadgesAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await _db.UserBadges
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .ToArrayAsync(cancellationToken);
    }

    public Task AddAsync<T>(T entity, CancellationToken cancellationToken = default)
        where T : class
    {
        _db.Set<T>().Add(entity);
        return Task.CompletedTask;
    }

    public Task AddRangeAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default)
        where T : class
    {
        _db.Set<T>().AddRange(entities);
        return Task.CompletedTask;
    }

    public Task RemoveAsync<T>(T entity, CancellationToken cancellationToken = default)
        where T : class
    {
        _db.Set<T>().Remove(entity);
        return Task.CompletedTask;
    }
}
