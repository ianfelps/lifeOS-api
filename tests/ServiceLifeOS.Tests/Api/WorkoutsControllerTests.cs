using Microsoft.AspNetCore.Mvc;
using ServiceLifeOS.Api.Controllers;
using ServiceLifeOS.Application.Ports;
using ServiceLifeOS.Application.Services;
using ServiceLifeOS.Domain.Entities;
using ServiceLifeOS.Dtos.Workouts;
using Xunit;

namespace ServiceLifeOS.Tests.Api;

public sealed class WorkoutsControllerTests
{
    [Fact]
    public async Task CreateExercise_UsesAuthenticatedUser()
    {
        var repository = new FakeWorkoutRepository();
        var controller = new WorkoutsController(
            new WorkoutService(repository, new FakeAuditLogRepository(), new FakeUnitOfWork()),
            new FakeCurrentUser());

        var result = await controller.CreateExercise(new() { Name = "Bench press" }, CancellationToken.None);

        Assert.IsType<CreatedResult>(result.Result);
        Assert.Equal("user-1", Assert.Single(repository.Exercises).UserId);
    }

    private sealed class FakeCurrentUser : ICurrentUser
    {
        public string UserId => "user-1";

        public string UserName => "user";

        public string TokenId => "token";
    }

    private sealed class FakeWorkoutRepository : IWorkoutRepository
    {
        public List<Exercise> Exercises { get; } = [];

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
            return Task.FromResult<WorkoutSheet?>(null);
        }

        public Task<IReadOnlyCollection<WorkoutSheet>> GetSheetsAsync(
            string userId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<WorkoutSheet>>([]);
        }

        public Task<IReadOnlyCollection<WorkoutSheetExercise>> GetSheetExercisesAsync(
            Guid sheetId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<WorkoutSheetExercise>>([]);
        }

        public Task<IReadOnlyCollection<WorkoutSheetExerciseSet>> GetSheetSetsAsync(
            IReadOnlyCollection<Guid> ids,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<WorkoutSheetExerciseSet>>([]);
        }

        public Task<WorkoutSession?> GetSessionAsync(
            string userId,
            Guid sessionId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<WorkoutSession?>(null);
        }

        public Task<IReadOnlyCollection<WorkoutSession>> GetSessionsAsync(
            string userId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<WorkoutSession>>([]);
        }

        public Task<IReadOnlyCollection<WorkoutSessionExercise>> GetSessionExercisesAsync(
            Guid sessionId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<WorkoutSessionExercise>>([]);
        }

        public Task<IReadOnlyCollection<WorkoutSessionSet>> GetSessionSetsAsync(
            IReadOnlyCollection<Guid> ids,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<WorkoutSessionSet>>([]);
        }

        public Task<WeightUnit?> GetPreferredWeightUnitAsync(
            string userId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<WeightUnit?>(null);
        }

        public Task<XpEventRule?> GetXpRuleAsync(
            string userId,
            XpEventType eventType,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<XpEventRule?>(null);
        }

        public Task<IReadOnlyCollection<XpLedgerEntry>> GetXpEntriesForSourceAsync(
            string userId,
            string sourceType,
            Guid sourceId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<XpLedgerEntry>>([]);
        }

        public Task<IReadOnlyCollection<XpLedgerEntry>> GetXpEntriesAsync(
            string userId,
            string sourceType,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<XpLedgerEntry>>([]);
        }

        public Task<IReadOnlyCollection<Badge>> GetBadgesAsync(
            string userId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<Badge>>([]);
        }

        public Task<IReadOnlyCollection<BadgeCriterion>> GetBadgeCriteriaAsync(
            IReadOnlyCollection<Guid> ids,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<BadgeCriterion>>([]);
        }

        public Task<IReadOnlyCollection<UserBadge>> GetUserBadgesAsync(
            string userId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<UserBadge>>([]);
        }

        public Task AddAsync<T>(T entity, CancellationToken cancellationToken = default)
            where T : class
        {
            if (entity is Exercise exercise)
            {
                Exercises.Add(exercise);
            }

            return Task.CompletedTask;
        }

        public Task AddRangeAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default)
            where T : class
        {
            return Task.CompletedTask;
        }

        public Task RemoveAsync<T>(T entity, CancellationToken cancellationToken = default)
            where T : class
        {
            return Task.CompletedTask;
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
