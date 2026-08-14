using Microsoft.EntityFrameworkCore;
using ServiceLifeOS.Application.Ports;
using ServiceLifeOS.Domain.Entities;

namespace ServiceLifeOS.Infrastructure.Persistence.Repositories;

public sealed class WorkoutRepository : IWorkoutRepository
{
    private readonly AppDbContext _db;
    public WorkoutRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<Exercise?> GetExerciseAsync(
        string userId,
        Guid exerciseId,
        CancellationToken cancellationToken = default)
    {
        return _db.Exercises.FirstOrDefaultAsync(
            x => x.UserId == userId && x.Id == exerciseId,
            cancellationToken);
    }

    public async Task<IReadOnlyCollection<Exercise>> GetExercisesAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await _db.Exercises
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .ToArrayAsync(cancellationToken);
    }

    public Task<WorkoutSheet?> GetSheetAsync(
        string userId,
        Guid sheetId,
        CancellationToken cancellationToken = default)
    {
        return _db.WorkoutSheets.FirstOrDefaultAsync(
            x => x.UserId == userId && x.Id == sheetId,
            cancellationToken);
    }

    public async Task<IReadOnlyCollection<WorkoutSheet>> GetSheetsAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await _db.WorkoutSheets
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<WorkoutSheetExercise>> GetSheetExercisesAsync(
        Guid sheetId,
        CancellationToken cancellationToken = default)
    {
        return await _db.WorkoutSheetExercises
            .Where(x => x.WorkoutSheetId == sheetId)
            .OrderBy(x => x.Position)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<WorkoutSheetExerciseSet>> GetSheetSetsAsync(
        IReadOnlyCollection<Guid> sheetExerciseIds,
        CancellationToken cancellationToken = default)
    {
        return await _db.WorkoutSheetExerciseSets
            .Where(x => sheetExerciseIds.Contains(x.WorkoutSheetExerciseId))
            .OrderBy(x => x.Position)
            .ToArrayAsync(cancellationToken);
    }

    public Task<WorkoutSession?> GetSessionAsync(
        string userId,
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        return _db.WorkoutSessions.FirstOrDefaultAsync(
            x => x.UserId == userId && x.Id == sessionId && x.DeletedAt == null,
            cancellationToken);
    }

    public async Task<IReadOnlyCollection<WorkoutSession>> GetSessionsAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await _db.WorkoutSessions
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<WorkoutSessionExercise>> GetSessionExercisesAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        return await _db.WorkoutSessionExercises
            .Where(x => x.WorkoutSessionId == sessionId)
            .OrderBy(x => x.Position)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<WorkoutSessionSet>> GetSessionSetsAsync(
        IReadOnlyCollection<Guid> sessionExerciseIds,
        CancellationToken cancellationToken = default)
    {
        return await _db.WorkoutSessionSets
            .Where(x => sessionExerciseIds.Contains(x.WorkoutSessionExerciseId))
            .OrderBy(x => x.Position)
            .ToArrayAsync(cancellationToken);
    }

    public Task<WeightUnit?> GetPreferredWeightUnitAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return _db.UserPreferences
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => (WeightUnit?)x.PreferredWeightUnit)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<XpEventRule?> GetXpRuleAsync(
        string userId,
        XpEventType eventType,
        CancellationToken cancellationToken = default)
    {
        return _db.XpEventRules
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.EventType == eventType, cancellationToken);
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
