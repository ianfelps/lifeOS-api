using ServiceLifeOS.Application.Ports;
using ServiceLifeOS.Application.Services;
using ServiceLifeOS.Domain.Entities;
using ServiceLifeOS.Dtos.Workouts;
using Xunit;

namespace ServiceLifeOS.Tests.Application;

public sealed class WorkoutServiceTests
{
    [Fact]
    public async Task StartSession_FromSheetCopiesExercisesAndPlannedSets()
    {
        var repository = new FakeWorkoutRepository();
        var exercise = repository.AddExercise();
        var service = CreateService(repository);
        var sheet = await service.CreateSheetAsync(
            "user-1",
            new()
            {
                Name = "Upper",
                Exercises = [new() { ExerciseId = exercise.Id, Sets = [new() { TargetRepetitions = 10 }] }]
            });

        var session = await service.StartSessionAsync("user-1", new() { WorkoutSheetId = sheet.Id });

        var set = Assert.Single(Assert.Single(session.Exercises).Sets);
        Assert.Equal(exercise.Name, session.Exercises.Single().ExerciseName);
        Assert.Equal(10, set.Repetitions);
        Assert.Equal(WeightUnit.Kilograms, set.WeightUnit);
    }

    [Fact]
    public async Task CompleteAndCancelSession_GrantsAndReversesXpAndBadge()
    {
        var repository = new FakeWorkoutRepository();
        repository.XpRules.Add(new() { UserId = "user-1", EventType = XpEventType.WorkoutCompleted, Amount = 25 });
        var badge = new Badge { UserId = "user-1" };
        repository.Badges.Add(badge);
        repository.BadgeCriteria.Add(
            new()
            {
                BadgeId = badge.Id,
                Type = BadgeCriterionType.WorkoutCompletionCount,
                TargetValue = 1
            });
        var service = CreateService(repository);
        var session = await service.StartSessionAsync(
            "user-1",
            new()
            {
                Exercises =
                [
                    new()
                    {
                        ExerciseName = "Row",
                        Sets = [new() { Weight = 60, WeightUnit = WeightUnit.Kilograms, Repetitions = 10 }]
                    }
                ]
            });

        await service.CompleteSessionAsync("user-1", session.Id);
        await service.CancelSessionAsync("user-1", session.Id);

        Assert.Contains(repository.XpEntries, x => x.Type == XpLedgerEntryType.Grant && x.Amount == 25);
        Assert.Contains(repository.XpEntries, x => x.Type == XpLedgerEntryType.Reversal && x.Amount == -25);
        Assert.Empty(repository.UserBadges);
    }

    [Fact]
    public async Task GetExerciseProgress_SeparatesUnitsAndIgnoresCancelledSessions()
    {
        var repository = new FakeWorkoutRepository();
        var exercise = repository.AddExercise();
        var service = CreateService(repository);
        var first = await service.StartSessionAsync(
            "user-1",
            new()
            {
                Exercises =
                [
                    new()
                    {
                        ExerciseId = exercise.Id,
                        Sets =
                        [
                            new() { Weight = 60, WeightUnit = WeightUnit.Kilograms, Repetitions = 10 },
                            new() { Weight = 135, WeightUnit = WeightUnit.Pounds, Repetitions = 8 }
                        ]
                    }
                ]
            });
        await service.CompleteSessionAsync("user-1", first.Id);
        var cancelled = await service.StartSessionAsync(
            "user-1",
            new()
            {
                Exercises =
                [
                    new()
                    {
                        ExerciseId = exercise.Id,
                        Sets = [new() { Weight = 100, WeightUnit = WeightUnit.Kilograms, Repetitions = 10 }]
                    }
                ]
            });
        await service.CancelSessionAsync("user-1", cancelled.Id);

        var progress = await service.GetExerciseProgressAsync("user-1", exercise.Id);

        Assert.Equal(2, progress.Items.Count);
        Assert.Contains(
            progress.Items,
            x => x.WeightUnit == WeightUnit.Kilograms && x.MaxWeight == 60 && x.TotalVolume == 600);
        Assert.Contains(
            progress.Items,
            x => x.WeightUnit == WeightUnit.Pounds && x.MaxWeight == 135 && x.TotalVolume == 1080);
    }

    [Fact]
    public async Task CreateSheet_RejectsExerciseOwnedByAnotherUser()
    {
        var repository = new FakeWorkoutRepository();
        var exercise = repository.AddExercise("user-2");
        var service = CreateService(repository);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.CreateSheetAsync(
                "user-1",
                new()
                {
                    Name = "Upper",
                    Exercises = [new() { ExerciseId = exercise.Id, Sets = [new() { TargetRepetitions = 10 }] }]
                }));
    }

    private static WorkoutService CreateService(FakeWorkoutRepository repository)
    {
        return new WorkoutService(repository, new FakeAuditLogRepository(), new FakeUnitOfWork());
    }

    private sealed class FakeWorkoutRepository : IWorkoutRepository
    {
        public List<Exercise> Exercises { get; } = [];
        public List<WorkoutSheet> Sheets { get; } = [];
        public List<WorkoutSheetExercise> SheetExercises { get; } = [];
        public List<WorkoutSheetExerciseSet> SheetSets { get; } = [];
        public List<WorkoutSession> Sessions { get; } = [];
        public List<WorkoutSessionExercise> SessionExercises { get; } = [];
        public List<WorkoutSessionSet> SessionSets { get; } = [];
        public List<XpEventRule> XpRules { get; } = [];
        public List<XpLedgerEntry> XpEntries { get; } = [];
        public List<Badge> Badges { get; } = [];
        public List<BadgeCriterion> BadgeCriteria { get; } = [];
        public List<UserBadge> UserBadges { get; } = [];
        public Exercise AddExercise(string userId = "user-1")
        {
            var value = new Exercise { UserId = userId, Name = "Bench press" };
            Exercises.Add(value);

            return value;
        }

        public Task<Exercise?> GetExerciseAsync(
            string userId,
            Guid exerciseId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Exercises.FirstOrDefault(x => x.UserId == userId && x.Id == exerciseId));
        }

        public Task<IReadOnlyCollection<Exercise>> GetExercisesAsync(
            string userId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<Exercise>>(Exercises.Where(x => x.UserId == userId).ToArray());
        }

        public Task<WorkoutSheet?> GetSheetAsync(
            string userId,
            Guid sheetId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Sheets.FirstOrDefault(x => x.UserId == userId && x.Id == sheetId));
        }

        public Task<IReadOnlyCollection<WorkoutSheet>> GetSheetsAsync(
            string userId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<WorkoutSheet>>(Sheets.Where(x => x.UserId == userId).ToArray());
        }

        public Task<IReadOnlyCollection<WorkoutSheetExercise>> GetSheetExercisesAsync(
            Guid sheetId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<WorkoutSheetExercise>>(
                SheetExercises.Where(x => x.WorkoutSheetId == sheetId).OrderBy(x => x.Position).ToArray());
        }

        public Task<IReadOnlyCollection<WorkoutSheetExerciseSet>> GetSheetSetsAsync(
            IReadOnlyCollection<Guid> ids,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<WorkoutSheetExerciseSet>>(
                SheetSets.Where(x => ids.Contains(x.WorkoutSheetExerciseId)).ToArray());
        }

        public Task<WorkoutSession?> GetSessionAsync(
            string userId,
            Guid sessionId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Sessions.FirstOrDefault(x => x.UserId == userId && x.Id == sessionId && x.DeletedAt is null));
        }

        public Task<IReadOnlyCollection<WorkoutSession>> GetSessionsAsync(
            string userId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<WorkoutSession>>(
                Sessions.Where(x => x.UserId == userId).ToArray());
        }

        public Task<IReadOnlyCollection<WorkoutSessionExercise>> GetSessionExercisesAsync(
            Guid sessionId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<WorkoutSessionExercise>>(
                SessionExercises.Where(x => x.WorkoutSessionId == sessionId).OrderBy(x => x.Position).ToArray());
        }

        public Task<IReadOnlyCollection<WorkoutSessionSet>> GetSessionSetsAsync(
            IReadOnlyCollection<Guid> ids,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<WorkoutSessionSet>>(
                SessionSets.Where(x => ids.Contains(x.WorkoutSessionExerciseId)).ToArray());
        }

        public Task<WeightUnit?> GetPreferredWeightUnitAsync(
            string userId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<WeightUnit?>(WeightUnit.Kilograms);
        }

        public Task<XpEventRule?> GetXpRuleAsync(
            string userId,
            XpEventType eventType,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(XpRules.FirstOrDefault(x => x.UserId == userId && x.EventType == eventType));
        }

        public Task<IReadOnlyCollection<XpLedgerEntry>> GetXpEntriesForSourceAsync(
            string userId,
            string sourceType,
            Guid sourceId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<XpLedgerEntry>>(
                XpEntries.Where(x => x.UserId == userId && x.SourceType == sourceType && x.SourceId == sourceId)
                    .ToArray());
        }

        public Task<IReadOnlyCollection<XpLedgerEntry>> GetXpEntriesAsync(
            string userId,
            string sourceType,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<XpLedgerEntry>>(
                XpEntries.Where(x => x.UserId == userId && x.SourceType == sourceType).ToArray());
        }

        public Task<IReadOnlyCollection<Badge>> GetBadgesAsync(
            string userId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<Badge>>(Badges.Where(x => x.UserId == userId).ToArray());
        }

        public Task<IReadOnlyCollection<BadgeCriterion>> GetBadgeCriteriaAsync(
            IReadOnlyCollection<Guid> ids,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<BadgeCriterion>>(
                BadgeCriteria.Where(x => ids.Contains(x.BadgeId)).ToArray());
        }

        public Task<IReadOnlyCollection<UserBadge>> GetUserBadgesAsync(
            string userId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<UserBadge>>(UserBadges.Where(x => x.UserId == userId).ToArray());
        }

        public Task AddAsync<T>(T entity, CancellationToken cancellationToken = default)
            where T : class
        {
            Add(entity);

            return Task.CompletedTask;
        }

        public Task AddRangeAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default)
            where T : class
        {
            foreach (var entity in entities)
            {
                Add(entity);
            }

            return Task.CompletedTask;
        }

        public Task RemoveAsync<T>(T entity, CancellationToken cancellationToken = default)
            where T : class
        {
            if (entity is WorkoutSheetExercise sheetExercise)
            {
                SheetExercises.Remove(sheetExercise);
            }

            if (entity is WorkoutSessionExercise sessionExercise)
            {
                SessionExercises.Remove(sessionExercise);
            }

            if (entity is UserBadge badge)
            {
                UserBadges.Remove(badge);
            }

            return Task.CompletedTask;
        }

        private void Add<T>(T entity)
            where T : class
        {
            switch (entity)
            {
                case Exercise value:
                    Exercises.Add(value);
                    break;
                case WorkoutSheet value:
                    Sheets.Add(value);
                    break;
                case WorkoutSheetExercise value:
                    SheetExercises.Add(value);
                    break;
                case WorkoutSheetExerciseSet value:
                    SheetSets.Add(value);
                    break;
                case WorkoutSession value:
                    Sessions.Add(value);
                    break;
                case WorkoutSessionExercise value:
                    SessionExercises.Add(value);
                    break;
                case WorkoutSessionSet value:
                    SessionSets.Add(value);
                    break;
                case XpLedgerEntry value:
                    XpEntries.Add(value);
                    break;
                case UserBadge value:
                    UserBadges.Add(value);
                    break;
                default:
                    throw new InvalidOperationException("Unsupported entity.");
            }
        }
    }

    private sealed class FakeAuditLogRepository : IAuditLogRepository
    {
        public Task CreateAsync(AuditLog auditLog, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<AuditLogPage> GetPageAsync(
            string userId,
            AuditLogFilter filter,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new AuditLogPage());
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(1);
        }

        public Task<IAppTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}
