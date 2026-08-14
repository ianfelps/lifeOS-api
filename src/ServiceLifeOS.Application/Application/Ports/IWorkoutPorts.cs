using ServiceLifeOS.Domain.Entities;

namespace ServiceLifeOS.Application.Ports;

public interface IWorkoutRepository
{
    Task<Exercise?> GetExerciseAsync(
        string userId,
        Guid exerciseId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Exercise>> GetExercisesAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<WorkoutSheet?> GetSheetAsync(
        string userId,
        Guid sheetId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<WorkoutSheet>> GetSheetsAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<WorkoutSheetExercise>> GetSheetExercisesAsync(
        Guid sheetId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<WorkoutSheetExerciseSet>> GetSheetSetsAsync(
        IReadOnlyCollection<Guid> sheetExerciseIds,
        CancellationToken cancellationToken = default);

    Task<WorkoutSession?> GetSessionAsync(
        string userId,
        Guid sessionId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<WorkoutSession>> GetSessionsAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<WorkoutSessionExercise>> GetSessionExercisesAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<WorkoutSessionSet>> GetSessionSetsAsync(
        IReadOnlyCollection<Guid> sessionExerciseIds,
        CancellationToken cancellationToken = default);

    Task<WeightUnit?> GetPreferredWeightUnitAsync(
        string userId,
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
