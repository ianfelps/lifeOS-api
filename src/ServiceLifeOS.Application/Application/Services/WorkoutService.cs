using System.Text.Json;
using ServiceLifeOS.Application.Ports;
using ServiceLifeOS.Domain.Entities;
using ServiceLifeOS.Dtos.Workouts;

namespace ServiceLifeOS.Application.Services;

public sealed class WorkoutService
{
    private const string SessionSourceType = "WorkoutSession";
    private readonly IWorkoutRepository _workouts;
    private readonly IAuditLogRepository _auditLogs;
    private readonly IUnitOfWork _unitOfWork;
    private readonly GamificationService? _gamification;
    public WorkoutService(
        IWorkoutRepository workouts,
        IAuditLogRepository auditLogs,
        IUnitOfWork unitOfWork,
        GamificationService? gamification = null)
    {
        _workouts = workouts;
        _auditLogs = auditLogs;
        _unitOfWork = unitOfWork;
        _gamification = gamification;
    }

    public async Task<IReadOnlyCollection<ExerciseResponseDto>> GetExercisesAsync(
        string userId,
        bool includeArchived,
        CancellationToken cancellationToken = default)
    {
        return (await _workouts.GetExercisesAsync(userId, cancellationToken))
            .Where(x => includeArchived || !x.Archived)
            .OrderBy(x => x.Name)
            .Select(MapExercise)
            .ToArray();
    }

    public async Task<ExerciseResponseDto> CreateExerciseAsync(
        string userId,
        ExerciseRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ValidateName(request.Name, "Exercise");
        var now = DateTime.UtcNow;
        var value = new Exercise
        {
            UserId = userId,
            Name = request.Name.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };

        await _workouts.AddAsync(value, cancellationToken);
        await AuditAsync(userId, AuditAction.Created, "Exercise", value.Id, null, value, now, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapExercise(value);
    }

    public async Task<ExerciseResponseDto> UpdateExerciseAsync(
        string userId,
        Guid exerciseId,
        ExerciseRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ValidateName(request.Name, "Exercise");
        var value = await RequiredExerciseAsync(userId, exerciseId, cancellationToken);
        var previous = value.Name;
        value.Name = request.Name.Trim();
        value.UpdatedAt = DateTime.UtcNow;
        await AuditAsync(userId, AuditAction.Updated, "Exercise", value.Id, previous, value, value.UpdatedAt, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapExercise(value);
    }

    public async Task ArchiveExerciseAsync(string userId, Guid exerciseId, CancellationToken cancellationToken = default)
    {
        var value = await RequiredExerciseAsync(userId, exerciseId, cancellationToken);
        if (value.Archived)
        {
            return;
        }

        value.Archived = true;
        value.UpdatedAt = DateTime.UtcNow;
        await AuditAsync(userId, AuditAction.Archived, "Exercise", value.Id, null, value, value.UpdatedAt, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<WorkoutSheetResponseDto>> GetSheetsAsync(string userId, bool includeArchived, CancellationToken cancellationToken = default)
    {
        var values = (await _workouts.GetSheetsAsync(userId, cancellationToken))
            .Where(x => includeArchived || !x.Archived)
            .OrderBy(x => x.Name)
            .ToArray();
        var result = new List<WorkoutSheetResponseDto>();

        foreach (var value in values)
        {
            result.Add(await MapSheetAsync(userId, value, cancellationToken));
        }

        return result;
    }

    public async Task<WorkoutSheetResponseDto> GetSheetAsync(string userId, Guid sheetId, CancellationToken cancellationToken = default)
    {
        var sheet = await RequiredSheetAsync(userId, sheetId, cancellationToken);
        return await MapSheetAsync(userId, sheet, cancellationToken);
    }

    public async Task<WorkoutSheetResponseDto> CreateSheetAsync(string userId, WorkoutSheetRequestDto request, CancellationToken cancellationToken = default)
    {
        await ValidateSheetAsync(userId, request, cancellationToken);
        var now = DateTime.UtcNow;
        var value = new WorkoutSheet { UserId = userId, Name = request.Name.Trim(), CreatedAt = now, UpdatedAt = now };
        await _workouts.AddAsync(value, cancellationToken);
        await AddSheetContentAsync(value.Id, request.Exercises, cancellationToken);
        await AuditAsync(userId, AuditAction.Created, "WorkoutSheet", value.Id, null, value, now, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await MapSheetAsync(userId, value, cancellationToken);
    }

    public async Task<WorkoutSheetResponseDto> UpdateSheetAsync(string userId, Guid sheetId, WorkoutSheetRequestDto request, CancellationToken cancellationToken = default)
    {
        await ValidateSheetAsync(userId, request, cancellationToken);
        var value = await RequiredSheetAsync(userId, sheetId, cancellationToken);
        var previous = await MapSheetAsync(userId, value, cancellationToken);
        var exercises = await _workouts.GetSheetExercisesAsync(value.Id, cancellationToken);
        foreach (var exercise in exercises)
        {
            await _workouts.RemoveAsync(exercise, cancellationToken);
        }

        value.Name = request.Name.Trim();
        value.UpdatedAt = DateTime.UtcNow;
        await AddSheetContentAsync(value.Id, request.Exercises, cancellationToken);
        await AuditAsync(userId, AuditAction.Updated, "WorkoutSheet", value.Id, previous, value, value.UpdatedAt, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await MapSheetAsync(userId, value, cancellationToken);
    }

    public async Task ArchiveSheetAsync(string userId, Guid sheetId, CancellationToken cancellationToken = default)
    {
        var value = await RequiredSheetAsync(userId, sheetId, cancellationToken);
        if (value.Archived)
        {
            return;
        }

        value.Archived = true;
        value.UpdatedAt = DateTime.UtcNow;
        await AuditAsync(userId, AuditAction.Archived, "WorkoutSheet", value.Id, null, value, value.UpdatedAt, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<PagedWorkoutSessionResponseDto> GetSessionsAsync(string userId, WorkoutSessionQueryDto query, CancellationToken cancellationToken = default)
    {
        ValidateQuery(query);
        var values = (await _workouts.GetSessionsAsync(userId, cancellationToken)).Where(x => x.DeletedAt is null);
        if (query.From.HasValue) values = values.Where(x => x.StartedAt >= query.From);
        if (query.To.HasValue) values = values.Where(x => x.StartedAt <= query.To);
        if (query.WorkoutSheetId.HasValue) values = values.Where(x => x.WorkoutSheetId == query.WorkoutSheetId);
        if (query.Status.HasValue) values = values.Where(x => x.Status == query.Status);

        var ordered = values.OrderByDescending(x => x.StartedAt).ThenByDescending(x => x.Id).ToArray();
        var items = new List<WorkoutSessionResponseDto>();
        foreach (var value in ordered.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize))
        {
            items.Add(await MapSessionAsync(value, cancellationToken));
        }

        return new() { Items = items, Page = query.Page, PageSize = query.PageSize, TotalCount = ordered.Length };
    }

    public async Task<WorkoutSessionResponseDto> GetSessionAsync(string userId, Guid sessionId, CancellationToken cancellationToken = default)
    {
        var session = await RequiredSessionAsync(userId, sessionId, cancellationToken);
        return await MapSessionAsync(session, cancellationToken);
    }
    public async Task<WorkoutSessionResponseDto> StartSessionAsync(string userId, StartWorkoutSessionRequestDto request, CancellationToken cancellationToken = default)
    {
        if (request.WorkoutSheetId.HasValue && request.Exercises.Count != 0)
        {
            throw new ArgumentException("A workout session must use either a sheet or exercises.");
        }

        var now = DateTime.UtcNow;
        var session = new WorkoutSession { UserId = userId, WorkoutSheetId = request.WorkoutSheetId, StartedAt = now, CreatedAt = now, UpdatedAt = now };
        await _workouts.AddAsync(session, cancellationToken);
        if (request.WorkoutSheetId.HasValue)
        {
            await CopySheetToSessionAsync(userId, session.Id, request.WorkoutSheetId.Value, cancellationToken);
        }
        else
        {
            await ValidateSessionExercisesAsync(userId, request.Exercises, true, cancellationToken);
            await AddSessionContentAsync(session.Id, request.Exercises, null, cancellationToken, userId);
        }

        await AuditAsync(userId, AuditAction.Created, "WorkoutSession", session.Id, null, session, now, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        if (_gamification is not null) await _gamification.RefreshAsync(userId, cancellationToken);
        return await MapSessionAsync(session, cancellationToken);
    }
    public async Task<WorkoutSessionResponseDto> UpdateSessionAsync(string userId, Guid sessionId, UpdateWorkoutSessionRequestDto request, CancellationToken cancellationToken = default)
    {
        var session = await RequiredSessionAsync(userId, sessionId, cancellationToken);
        if (session.Status == WorkoutSessionStatus.Cancelled) throw new InvalidOperationException("Cancelled workout sessions cannot be edited.");
        await ValidateSessionExercisesAsync(userId, request.Exercises, true, cancellationToken);
        var previous = await MapSessionAsync(session, cancellationToken);
        var exercises = await _workouts.GetSessionExercisesAsync(session.Id, cancellationToken);
        foreach (var exercise in exercises)
        {
            await _workouts.RemoveAsync(exercise, cancellationToken);
        }

        await AddSessionContentAsync(session.Id, request.Exercises, null, cancellationToken, userId);
        session.UpdatedAt = DateTime.UtcNow;
        await AuditAsync(userId, AuditAction.Updated, "WorkoutSession", session.Id, previous, session, session.UpdatedAt, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        if (_gamification is not null) await _gamification.RefreshAsync(userId, cancellationToken);
        return await MapSessionAsync(session, cancellationToken);
    }
    public async Task<WorkoutSessionResponseDto> CompleteSessionAsync(string userId, Guid sessionId, CancellationToken cancellationToken = default)
    {
        var session = await RequiredSessionAsync(userId, sessionId, cancellationToken);
        if (session.Status != WorkoutSessionStatus.Draft)
        {
            throw new InvalidOperationException("Only draft workout sessions can be completed.");
        }

        var exercises = await _workouts.GetSessionExercisesAsync(session.Id, cancellationToken);
        if (exercises.Count == 0)
        {
            throw new ArgumentException("Workout sessions require at least one exercise.");
        }

        var now = DateTime.UtcNow;
        session.Status = WorkoutSessionStatus.Completed;
        session.CompletedAt = now;
        session.UpdatedAt = now;
        await SyncXpAsync(userId, session, now, cancellationToken);
        await RecalculateBadgesAsync(userId, now, cancellationToken);
        await AuditAsync(userId, AuditAction.Updated, "WorkoutSession", session.Id, null, session, now, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        if (_gamification is not null) await _gamification.RefreshAsync(userId, cancellationToken);
        return await MapSessionAsync(session, cancellationToken);
    }
    public async Task CancelSessionAsync(string userId, Guid sessionId, CancellationToken cancellationToken = default)
    {
        var session = await RequiredSessionAsync(userId, sessionId, cancellationToken);
        if (session.Status == WorkoutSessionStatus.Cancelled)
        {
            return;
        }

        var now = DateTime.UtcNow;
        session.Status = WorkoutSessionStatus.Cancelled;
        session.CancelledAt = now;
        session.UpdatedAt = now;
        await SyncXpAsync(userId, session, now, cancellationToken);
        await RecalculateBadgesAsync(userId, now, cancellationToken);
        await AuditAsync(userId, AuditAction.Updated, "WorkoutSession", session.Id, null, session, now, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        if (_gamification is not null) await _gamification.RefreshAsync(userId, cancellationToken);
    }
    public async Task DeleteSessionAsync(string userId, Guid sessionId, CancellationToken cancellationToken = default)
    {
        var session = await RequiredSessionAsync(userId, sessionId, cancellationToken);
        var now = DateTime.UtcNow;
        session.DeletedAt = now;
        session.UpdatedAt = now;
        await SyncXpAsync(userId, session, now, cancellationToken);
        await RecalculateBadgesAsync(userId, now, cancellationToken);
        await AuditAsync(userId, AuditAction.Deleted, "WorkoutSession", session.Id, session, null, now, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        if (_gamification is not null) await _gamification.RefreshAsync(userId, cancellationToken);
    }
    public async Task<ExerciseProgressResponseDto> GetExerciseProgressAsync(string userId, Guid exerciseId, CancellationToken cancellationToken = default)
    {
        var exercise = await RequiredExerciseAsync(userId, exerciseId, cancellationToken);
        var sessions = (await _workouts.GetSessionsAsync(userId, cancellationToken))
            .Where(x => x.Status == WorkoutSessionStatus.Completed && x.DeletedAt is null && x.CompletedAt.HasValue)
            .ToArray();
        var result = new List<ExerciseProgressItemDto>();
        foreach (var session in sessions)
        {
            var sessionExercises = await _workouts.GetSessionExercisesAsync(session.Id, cancellationToken);
            foreach (var sessionExercise in sessionExercises.Where(x => x.ExerciseId == exerciseId))
            {
                var sets = (await _workouts.GetSessionSetsAsync([sessionExercise.Id], cancellationToken))
                    .Where(x => x.Weight.HasValue && x.WeightUnit.HasValue && x.Repetitions.HasValue)
                    .ToArray();
                foreach (var group in sets.GroupBy(x => x.WeightUnit!.Value))
                {
                    var best = group.OrderByDescending(x => x.Weight).ThenByDescending(x => x.Repetitions).First();
                    result.Add(new()
                    {
                        SessionId = session.Id,
                        CompletedAt = session.CompletedAt!.Value,
                        WeightUnit = group.Key,
                        MaxWeight = group.Max(x => x.Weight!.Value),
                        BestSetWeight = best.Weight!.Value,
                        BestSetRepetitions = best.Repetitions!.Value,
                        TotalVolume = group.Sum(x => x.Weight!.Value * x.Repetitions!.Value)
                    });
                }
            }
        }
        return new() { ExerciseId = exercise.Id, ExerciseName = exercise.Name, Items = result.OrderBy(x => x.CompletedAt).ToArray() };
    }

    private async Task CopySheetToSessionAsync(string userId, Guid sessionId, Guid sheetId, CancellationToken cancellationToken)
    {
        var sheet = await RequiredSheetAsync(userId, sheetId, cancellationToken);
        if (sheet.Archived)
        {
            throw new InvalidOperationException("Archived workout sheets cannot be used.");
        }

        var sheetExercises = await _workouts.GetSheetExercisesAsync(sheet.Id, cancellationToken);
        var sets = await _workouts.GetSheetSetsAsync(sheetExercises.Select(x => x.Id).ToArray(), cancellationToken);
        var preferredUnit = await _workouts.GetPreferredWeightUnitAsync(userId, cancellationToken);
        foreach (var sheetExercise in sheetExercises)
        {
            var exercise = await RequiredExerciseAsync(userId, sheetExercise.ExerciseId, cancellationToken);
            var sessionExercise = new WorkoutSessionExercise
            {
                WorkoutSessionId = sessionId,
                ExerciseId = exercise.Id,
                ExerciseName = exercise.Name,
                Position = sheetExercise.Position
            };
            await _workouts.AddAsync(sessionExercise, cancellationToken);
            await _workouts.AddRangeAsync(
                sets.Where(x => x.WorkoutSheetExerciseId == sheetExercise.Id).Select(x => new WorkoutSessionSet
                {
                    WorkoutSessionExerciseId = sessionExercise.Id,
                    Position = x.Position,
                    WeightUnit = preferredUnit,
                    Repetitions = x.TargetRepetitions
                }),
                cancellationToken);
        }
    }

    private async Task AddSheetContentAsync(Guid sheetId, IEnumerable<WorkoutSheetExerciseRequestDto> exercises, CancellationToken cancellationToken)
    {
        var values = new List<WorkoutSheetExercise>();
        foreach (var (request, index) in exercises.Select((x, i) => (x, i)))
        {
            values.Add(new WorkoutSheetExercise { WorkoutSheetId = sheetId, ExerciseId = request.ExerciseId, Position = index + 1 });
        }

        await _workouts.AddRangeAsync(values, cancellationToken);
        foreach (var (request, exercise) in exercises.Zip(values))
        {
            await _workouts.AddRangeAsync(
                request.Sets.Select((x, i) => new WorkoutSheetExerciseSet { WorkoutSheetExerciseId = exercise.Id, Position = i + 1, TargetRepetitions = x.TargetRepetitions }),
                cancellationToken);
        }
    }
    private async Task AddSessionContentAsync(Guid sessionId, IEnumerable<WorkoutSessionExerciseRequestDto> exercises, WeightUnit? defaultUnit, CancellationToken cancellationToken, string? userId = null)
    {
        foreach (var (request, index) in exercises.Select((x, i) => (x, i)))
        {
            var name = request.ExerciseName?.Trim();
            if (request.ExerciseId.HasValue)
            {
                var ownerId = userId ?? throw new InvalidOperationException("Workout session user was not found.");
                name = (await RequiredExerciseAsync(ownerId, request.ExerciseId.Value, cancellationToken)).Name;
            }

            var exercise = new WorkoutSessionExercise { WorkoutSessionId = sessionId, ExerciseId = request.ExerciseId, ExerciseName = name!, Position = index + 1 };
            await _workouts.AddAsync(exercise, cancellationToken);
            await _workouts.AddRangeAsync(
                request.Sets.Select((x, i) => new WorkoutSessionSet
                {
                    WorkoutSessionExerciseId = exercise.Id,
                    Position = i + 1,
                    Weight = x.Weight,
                    WeightUnit = x.WeightUnit ?? (x.Weight.HasValue ? defaultUnit : null),
                    Repetitions = x.Repetitions
                }),
                cancellationToken);
        }
    }

    private async Task ValidateSheetAsync(string userId, WorkoutSheetRequestDto request, CancellationToken cancellationToken)
    {
        ValidateName(request.Name, "Workout sheet");
        if (request.Exercises.Count == 0 || request.Exercises.Select(x => x.ExerciseId).Distinct().Count() != request.Exercises.Count || request.Exercises.Any(x => x.Sets.Count == 0 || x.Sets.Any(y => y.TargetRepetitions < 1))) throw new ArgumentException("Workout sheet exercises are invalid.");
        foreach (var requestExercise in request.Exercises)
        {
            if ((await RequiredExerciseAsync(userId, requestExercise.ExerciseId, cancellationToken)).Archived) throw new ArgumentException("Archived exercises cannot be used in workout sheets.");
        }
    }

    private async Task ValidateSessionExercisesAsync(string userId, IReadOnlyCollection<WorkoutSessionExerciseRequestDto> exercises, bool requireExercises, CancellationToken cancellationToken)
    {
        if (requireExercises && exercises.Count == 0) throw new ArgumentException("Workout sessions require at least one exercise.");
        foreach (var exercise in exercises)
        {
            if (exercise.Sets.Count == 0 || (!exercise.ExerciseId.HasValue && string.IsNullOrWhiteSpace(exercise.ExerciseName)) || (exercise.ExerciseName?.Trim().Length > 120)) throw new ArgumentException("Workout session exercises are invalid.");
            if (exercise.ExerciseId.HasValue) await RequiredExerciseAsync(userId, exercise.ExerciseId.Value, cancellationToken);
            foreach (var set in exercise.Sets)
            {
                if ((set.Weight.HasValue != set.WeightUnit.HasValue) || (set.Weight.HasValue && (set.Weight <= 0 || !Enum.IsDefined(set.WeightUnit!.Value))) || (set.Repetitions.HasValue && set.Repetitions < 1)) throw new ArgumentException("Workout session sets are invalid.");
            }
        }
    }
    private async Task<Exercise> RequiredExerciseAsync(string userId, Guid exerciseId, CancellationToken cancellationToken) => await _workouts.GetExerciseAsync(userId, exerciseId, cancellationToken) ?? throw new KeyNotFoundException("Exercise was not found.");
    private async Task<WorkoutSheet> RequiredSheetAsync(string userId, Guid sheetId, CancellationToken cancellationToken) => await _workouts.GetSheetAsync(userId, sheetId, cancellationToken) ?? throw new KeyNotFoundException("Workout sheet was not found.");
    private async Task<WorkoutSession> RequiredSessionAsync(string userId, Guid sessionId, CancellationToken cancellationToken) => await _workouts.GetSessionAsync(userId, sessionId, cancellationToken) ?? throw new KeyNotFoundException("Workout session was not found.");
    private async Task<WorkoutSheetResponseDto> MapSheetAsync(string userId, WorkoutSheet value, CancellationToken cancellationToken)
    {
        var exercises = await _workouts.GetSheetExercisesAsync(value.Id, cancellationToken);
        var sets = await _workouts.GetSheetSetsAsync(exercises.Select(x => x.Id).ToArray(), cancellationToken);
        var catalog = await _workouts.GetExercisesAsync(userId, cancellationToken);
        return new()
        {
            Id = value.Id,
            Name = value.Name,
            Archived = value.Archived,
            Exercises = exercises.Select(x => new WorkoutSheetExerciseResponseDto
            {
                Id = x.Id,
                ExerciseId = x.ExerciseId,
                ExerciseName = catalog.FirstOrDefault(y => y.Id == x.ExerciseId)?.Name ?? string.Empty,
                Position = x.Position,
                Sets = sets.Where(y => y.WorkoutSheetExerciseId == x.Id).Select(y => new WorkoutSheetSetResponseDto
                {
                    Id = y.Id,
                    Position = y.Position,
                    TargetRepetitions = y.TargetRepetitions
                }).ToArray()
            }).ToArray()
        };
    }

    private async Task<WorkoutSessionResponseDto> MapSessionAsync(WorkoutSession value, CancellationToken cancellationToken)
    {
        var exercises = await _workouts.GetSessionExercisesAsync(value.Id, cancellationToken);
        var sets = await _workouts.GetSessionSetsAsync(exercises.Select(x => x.Id).ToArray(), cancellationToken);
        return new()
        {
            Id = value.Id,
            WorkoutSheetId = value.WorkoutSheetId,
            Status = value.Status,
            StartedAt = value.StartedAt,
            CompletedAt = value.CompletedAt,
            CancelledAt = value.CancelledAt,
            Exercises = exercises.Select(x => new WorkoutSessionExerciseResponseDto
            {
                Id = x.Id,
                ExerciseId = x.ExerciseId,
                ExerciseName = x.ExerciseName,
                Position = x.Position,
                Sets = sets.Where(y => y.WorkoutSessionExerciseId == x.Id).Select(y => new WorkoutSessionSetResponseDto
                {
                    Id = y.Id,
                    Position = y.Position,
                    Weight = y.Weight,
                    WeightUnit = y.WeightUnit,
                    Repetitions = y.Repetitions
                }).ToArray()
            }).ToArray()
        };
    }
    private async Task SyncXpAsync(string userId, WorkoutSession session, DateTime now, CancellationToken cancellationToken)
    {
        var entries = await _workouts.GetXpEntriesForSourceAsync(userId, SessionSourceType, session.Id, cancellationToken);
        var grant = entries.FirstOrDefault(x => x.Type == XpLedgerEntryType.Grant && !entries.Any(y => y.Type == XpLedgerEntryType.Reversal && y.ReversedEntryId == x.Id));
        var qualifies = session.Status == WorkoutSessionStatus.Completed && session.DeletedAt is null;
        if (qualifies && grant is null)
        {
            var rule = await _workouts.GetXpRuleAsync(userId, XpEventType.WorkoutCompleted, cancellationToken);
            if (rule is not null)
            {
                await _workouts.AddAsync(new XpLedgerEntry { UserId = userId, Type = XpLedgerEntryType.Grant, Amount = rule.Amount, EventType = XpEventType.WorkoutCompleted, SourceType = SessionSourceType, SourceId = session.Id, CreatedAt = now }, cancellationToken);
            }
        }

        if (!qualifies && grant is not null)
        {
            await _workouts.AddAsync(new XpLedgerEntry { UserId = userId, Type = XpLedgerEntryType.Reversal, Amount = -grant.Amount, EventType = XpEventType.WorkoutCompleted, SourceType = SessionSourceType, SourceId = session.Id, ReversedEntryId = grant.Id, CreatedAt = now }, cancellationToken);
        }
    }

    private async Task RecalculateBadgesAsync(string userId, DateTime now, CancellationToken cancellationToken)
    {
        var badges = await _workouts.GetBadgesAsync(userId, cancellationToken);
        var criteria = await _workouts.GetBadgeCriteriaAsync(badges.Select(x => x.Id).ToArray(), cancellationToken);
        var entries = await _workouts.GetXpEntriesAsync(userId, SessionSourceType, cancellationToken);
        var count = entries.Count(x => x.Type == XpLedgerEntryType.Grant && !entries.Any(y => y.Type == XpLedgerEntryType.Reversal && y.ReversedEntryId == x.Id));
        var unlocked = await _workouts.GetUserBadgesAsync(userId, cancellationToken);
        foreach (var badge in badges)
        {
            var badgeCriteria = criteria.Where(x => x.BadgeId == badge.Id).ToArray();
            if (badgeCriteria.Length == 0 || badgeCriteria.Any(x => x.Type != BadgeCriterionType.WorkoutCompletionCount)) continue;
            var existing = unlocked.FirstOrDefault(x => x.BadgeId == badge.Id);
            if (badgeCriteria.All(x => count >= x.TargetValue) && existing is null)
            {
                await _workouts.AddAsync(new UserBadge { UserId = userId, BadgeId = badge.Id, UnlockedAt = now }, cancellationToken);
            }

            if (badgeCriteria.Any(x => count < x.TargetValue) && existing is not null)
            {
                await _workouts.RemoveAsync(existing, cancellationToken);
            }
        }
    }
    private async Task AuditAsync(string userId, AuditAction action, string resourceType, Guid resourceId, object? previous, object? current, DateTime now, CancellationToken cancellationToken) => await _auditLogs.CreateAsync(new AuditLog { UserId = userId, Action = action, ResourceType = resourceType, ResourceId = resourceId, PreviousValues = previous is null ? null : JsonSerializer.Serialize(previous), CurrentValues = current is null ? null : JsonSerializer.Serialize(current), CreatedAt = now }, cancellationToken);
    private static ExerciseResponseDto MapExercise(Exercise value) => new() { Id = value.Id, Name = value.Name, Archived = value.Archived };
    private static void ValidateName(string value, string resource) { if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > 120) throw new ArgumentException($"{resource} name is invalid."); }
    private static void ValidateQuery(WorkoutSessionQueryDto query) { if (query.Page < 1 || query.PageSize is < 1 or > 100 || query.From > query.To || (query.Status.HasValue && !Enum.IsDefined(query.Status.Value))) throw new ArgumentException("Workout session query is invalid."); }
}
