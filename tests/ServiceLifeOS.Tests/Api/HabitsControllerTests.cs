using Microsoft.AspNetCore.Mvc;
using ServiceLifeOS.Api.Controllers;
using ServiceLifeOS.Application.Ports;
using ServiceLifeOS.Application.Services;
using ServiceLifeOS.Domain.Entities;
using ServiceLifeOS.Dtos.Habits;
using Xunit;

namespace ServiceLifeOS.Tests.Api;

public sealed class HabitsControllerTests
{
    [Fact]
    public async Task CreateHabit_ReturnsCreatedHabitForAuthenticatedUser()
    {
        var repository = new FakeHabitRepository();
        var controller = new HabitsController(
            new HabitService(repository, new FakeAuditLogRepository(), new FakeUnitOfWork()),
            new FakeCurrentUser());

        var result = await controller.CreateHabit(
            new()
            {
                Title = "Read",
                Priority = HabitPriority.Medium,
                Schedule = new() { Type = HabitScheduleType.Daily }
            },
            CancellationToken.None);

        var response = Assert.IsType<CreatedResult>(result.Result);
        Assert.Equal("user-1", Assert.Single(repository.Habits).UserId);
        Assert.Equal("Read", Assert.IsType<HabitResponseDto>(response.Value).Title);
    }

    private sealed class FakeCurrentUser : ICurrentUser
    {
        public string UserId => "user-1";
        public string UserName => "user";
        public string TokenId => "token";
    }

    private sealed class FakeHabitRepository : IHabitRepository
    {
        public List<Habit> Habits { get; } = [];
        public List<HabitSchedule> Schedules { get; } = [];
        public List<HabitScheduleWeekday> Weekdays { get; } = [];
        public Task<Habit?> GetHabitAsync(
            string userId,
            Guid habitId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Habits.FirstOrDefault(x => x.UserId == userId && x.Id == habitId));
        }

        public Task<IReadOnlyCollection<Habit>> GetHabitsAsync(
            string userId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<Habit>>(
                Habits.Where(x => x.UserId == userId).ToArray());
        }

        public Task<HabitSchedule?> GetScheduleAsync(
            Guid habitId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Schedules.FirstOrDefault(x => x.HabitId == habitId));
        }

        public Task<IReadOnlyCollection<HabitScheduleWeekday>> GetWeekdaysAsync(
            Guid scheduleId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<HabitScheduleWeekday>>(
                Weekdays.Where(x => x.HabitScheduleId == scheduleId).ToArray());
        }
        public Task<IReadOnlyCollection<HabitCompletion>> GetCompletionsAsync(string userId, Guid habitId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<HabitCompletion>>([]);
        public Task<HabitCompletion?> GetCompletionAsync(string userId, Guid habitId, Guid completionId, CancellationToken cancellationToken = default) => Task.FromResult<HabitCompletion?>(null);
        public Task<XpEventRule?> GetXpRuleAsync(string userId, XpEventType eventType, CancellationToken cancellationToken = default) => Task.FromResult<XpEventRule?>(null);
        public Task<IReadOnlyCollection<XpLedgerEntry>> GetXpEntriesForSourceAsync(string userId, string sourceType, Guid sourceId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<XpLedgerEntry>>([]);
        public Task<IReadOnlyCollection<XpLedgerEntry>> GetXpEntriesAsync(string userId, string sourceType, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<XpLedgerEntry>>([]);
        public Task<IReadOnlyCollection<Badge>> GetBadgesAsync(string userId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<Badge>>([]);
        public Task<IReadOnlyCollection<BadgeCriterion>> GetBadgeCriteriaAsync(IReadOnlyCollection<Guid> badgeIds, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<BadgeCriterion>>([]);
        public Task<IReadOnlyCollection<UserBadge>> GetUserBadgesAsync(string userId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<UserBadge>>([]);
        public Task AddAsync<T>(T entity, CancellationToken cancellationToken = default) where T : class { if (entity is Habit habit) Habits.Add(habit); if (entity is HabitSchedule schedule) Schedules.Add(schedule); return Task.CompletedTask; }
        public Task AddRangeAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default) where T : class { foreach (var entity in entities) if (entity is HabitScheduleWeekday weekday) Weekdays.Add(weekday); return Task.CompletedTask; }
        public Task RemoveAsync<T>(T entity, CancellationToken cancellationToken = default) where T : class => Task.CompletedTask;
    }

    private sealed class FakeAuditLogRepository : IAuditLogRepository
    {
        public Task CreateAsync(AuditLog auditLog, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<AuditLogPage> GetPageAsync(string userId, AuditLogFilter filter, CancellationToken cancellationToken = default) => Task.FromResult(new AuditLogPage());
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
        public Task<IAppTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
