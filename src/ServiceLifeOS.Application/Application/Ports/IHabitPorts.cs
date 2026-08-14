using ServiceLifeOS.Domain.Entities;

namespace ServiceLifeOS.Application.Ports;

public interface IHabitRepository
{
    Task<Habit?> GetHabitAsync(
        string userId,
        Guid habitId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Habit>> GetHabitsAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<HabitSchedule?> GetScheduleAsync(
        Guid habitId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<HabitScheduleWeekday>> GetWeekdaysAsync(
        Guid scheduleId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<HabitCompletion>> GetCompletionsAsync(
        string userId,
        Guid habitId,
        CancellationToken cancellationToken = default);

    Task<HabitCompletion?> GetCompletionAsync(
        string userId,
        Guid habitId,
        Guid completionId,
        CancellationToken cancellationToken = default);

    Task<XpEventRule?> GetXpRuleAsync(
        string userId,
        XpEventType eventType,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<XpLedgerEntry>> GetXpEntriesForSourceAsync(
        string userId,
        string sourceType,
        Guid sourceId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<XpLedgerEntry>> GetXpEntriesAsync(
        string userId,
        string sourceType,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Badge>> GetBadgesAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<BadgeCriterion>> GetBadgeCriteriaAsync(
        IReadOnlyCollection<Guid> badgeIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<UserBadge>> GetUserBadgesAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task AddAsync<T>(T entity, CancellationToken cancellationToken = default)
        where T : class;

    Task AddRangeAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default)
        where T : class;

    Task RemoveAsync<T>(T entity, CancellationToken cancellationToken = default)
        where T : class;
}
