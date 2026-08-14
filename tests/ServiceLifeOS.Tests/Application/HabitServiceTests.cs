using ServiceLifeOS.Application.Ports;
using ServiceLifeOS.Application.Services;
using ServiceLifeOS.Domain.Entities;
using ServiceLifeOS.Dtos.Habits;
using Xunit;

namespace ServiceLifeOS.Tests.Application;

public sealed class HabitServiceTests
{
    [Fact]
    public async Task CreateHabit_WeekdaySchedulePersistsConfiguredWeekdays()
    {
        var repository = new FakeHabitRepository();
        var service = CreateService(repository);

        var habit = await service.CreateHabitAsync("user-1", new()
        {
            Title = "Read",
            Priority = HabitPriority.Medium,
            Schedule = new()
            {
                Type = HabitScheduleType.Weekdays,
                Weekdays = [DayOfWeek.Monday, DayOfWeek.Friday]
            }
        });

        Assert.Equal(HabitScheduleType.Weekdays, habit.Schedule.Type);
        Assert.Equal([DayOfWeek.Monday, DayOfWeek.Friday], habit.Schedule.Weekdays);
        Assert.Equal(2, repository.Weekdays.Count);
    }

    [Fact]
    public async Task CreateHabit_InvalidDailyCountIsRejected()
    {
        var service = CreateService(new FakeHabitRepository());

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateHabitAsync("user-1", new()
            {
                Title = "Drink water",
                Schedule = new() { Type = HabitScheduleType.DailyCount, TargetCount = 0 }
            }));
    }

    [Fact]
    public async Task CreateCompletion_DailyCountGrantsXpAndRejectsExcess()
    {
        var repository = new FakeHabitRepository();
        repository.XpRules.Add(new() { UserId = "user-1", EventType = XpEventType.HabitCompletion, Amount = 5 });
        var habit = await AddHabitAsync(repository, HabitScheduleType.DailyCount, 2);
        var service = CreateService(repository);
        var today = LocalToday();

        await service.CreateCompletionAsync("user-1", habit.Id, new() { CompletedOn = today });
        await service.CreateCompletionAsync("user-1", habit.Id, new() { CompletedOn = today });

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateCompletionAsync("user-1", habit.Id, new() { CompletedOn = today }));
        Assert.Equal(
            2,
            repository.XpEntries.Count(
                x => x.Type == XpLedgerEntryType.Grant && x.EventType == XpEventType.HabitCompletion));
    }

    [Fact]
    public async Task DeleteCompletion_ReversesCompletionAndWeeklyGoalXp()
    {
        var repository = new FakeHabitRepository();
        repository.XpRules.AddRange([
            new XpEventRule { UserId = "user-1", EventType = XpEventType.HabitCompletion, Amount = 5 },
            new XpEventRule { UserId = "user-1", EventType = XpEventType.WeeklyHabitGoal, Amount = 20 }
        ]);
        var habit = await AddHabitAsync(repository, HabitScheduleType.WeeklyCount, 2);
        var service = CreateService(repository);
        var today = LocalToday();

        var first = await service.CreateCompletionAsync("user-1", habit.Id, new() { CompletedOn = today });
        await service.CreateCompletionAsync("user-1", habit.Id, new() { CompletedOn = today });
        await service.DeleteCompletionAsync("user-1", habit.Id, first.Id);

        Assert.Equal(2, repository.XpEntries.Count(x => x.Type == XpLedgerEntryType.Reversal));
    }

    [Fact]
    public async Task GetProgress_WeekdayScheduleIgnoresDaysOutsideScheduleForStreak()
    {
        var repository = new FakeHabitRepository();
        var habit = await AddHabitAsync(
            repository,
            HabitScheduleType.Weekdays,
            1,
            [DayOfWeek.Monday, DayOfWeek.Wednesday]);
        var service = CreateService(repository);
        var monday = LocalToday().AddDays(-((int)LocalToday().DayOfWeek + 6) % 7);
        repository.Completions.AddRange([
            new HabitCompletion { UserId = "user-1", HabitId = habit.Id, CompletedOn = monday },
            new HabitCompletion { UserId = "user-1", HabitId = habit.Id, CompletedOn = monday.AddDays(2) }
        ]);

        var progress = await service.GetProgressAsync("user-1", habit.Id, monday.AddDays(2));

        Assert.Equal(2, progress.Streak);
    }

    [Fact]
    public async Task ArchiveHabit_PreservesHistoryAndRemovesPendingHabit()
    {
        var repository = new FakeHabitRepository();
        var habit = await AddHabitAsync(repository, HabitScheduleType.Daily, 1);
        repository.Completions.Add(new() { UserId = "user-1", HabitId = habit.Id, CompletedOn = LocalToday().AddDays(-1) });
        var service = CreateService(repository);

        await service.ArchiveHabitAsync("user-1", habit.Id);

        Assert.Equal(HabitStatus.Archived, habit.Status);
        Assert.Single(repository.Completions);
        Assert.Empty(await service.GetPendingHabitsAsync("user-1", LocalToday()));
    }

    [Fact]
    public async Task CreateCompletion_RejectsOtherUsersHabit()
    {
        var repository = new FakeHabitRepository();
        var habit = await AddHabitAsync(repository, HabitScheduleType.Daily, 1, userId: "user-2");
        var service = CreateService(repository);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.CreateCompletionAsync("user-1", habit.Id, new() { CompletedOn = LocalToday() }));
    }

    private static HabitService CreateService(FakeHabitRepository repository)
    {
        return new HabitService(repository, new FakeAuditLogRepository(), new FakeUnitOfWork());
    }

    private static async Task<Habit> AddHabitAsync(
        FakeHabitRepository repository,
        HabitScheduleType type,
        int targetCount,
        IReadOnlyCollection<DayOfWeek>? weekdays = null,
        string userId = "user-1")
    {
        var habit = new Habit { UserId = userId, Title = "Habit", Status = HabitStatus.Active };
        var schedule = new HabitSchedule { HabitId = habit.Id, Type = type, TargetCount = targetCount };
        repository.Habits.Add(habit);
        repository.Schedules.Add(schedule);
        if (weekdays is not null)
        {
            repository.Weekdays.AddRange(
                weekdays.Select(x => new HabitScheduleWeekday
                {
                    HabitScheduleId = schedule.Id,
                    DayOfWeek = x
                }));
        }
        await Task.CompletedTask;
        return habit;
    }

    private static DateOnly LocalToday() => DateOnly.FromDateTime(
        TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "America/Sao_Paulo"));

    private sealed class FakeHabitRepository : IHabitRepository
    {
        public List<Habit> Habits { get; } = [];
        public List<HabitSchedule> Schedules { get; } = [];
        public List<HabitScheduleWeekday> Weekdays { get; } = [];
        public List<HabitCompletion> Completions { get; } = [];
        public List<XpEventRule> XpRules { get; } = [];
        public List<XpLedgerEntry> XpEntries { get; } = [];
        public List<Badge> Badges { get; } = [];
        public List<BadgeCriterion> BadgeCriteria { get; } = [];
        public List<UserBadge> UserBadges { get; } = [];
        public Task<Habit?> GetHabitAsync(string userId, Guid habitId, CancellationToken cancellationToken = default) => Task.FromResult(Habits.FirstOrDefault(x => x.UserId == userId && x.Id == habitId));
        public Task<IReadOnlyCollection<Habit>> GetHabitsAsync(string userId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<Habit>>(Habits.Where(x => x.UserId == userId).ToArray());
        public Task<HabitSchedule?> GetScheduleAsync(Guid habitId, CancellationToken cancellationToken = default) => Task.FromResult(Schedules.FirstOrDefault(x => x.HabitId == habitId));
        public Task<IReadOnlyCollection<HabitScheduleWeekday>> GetWeekdaysAsync(Guid scheduleId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<HabitScheduleWeekday>>(Weekdays.Where(x => x.HabitScheduleId == scheduleId).ToArray());
        public Task<IReadOnlyCollection<HabitCompletion>> GetCompletionsAsync(string userId, Guid habitId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<HabitCompletion>>(Completions.Where(x => x.UserId == userId && x.HabitId == habitId).ToArray());
        public Task<HabitCompletion?> GetCompletionAsync(string userId, Guid habitId, Guid completionId, CancellationToken cancellationToken = default) => Task.FromResult(Completions.FirstOrDefault(x => x.UserId == userId && x.HabitId == habitId && x.Id == completionId && x.DeletedAt is null));
        public Task<XpEventRule?> GetXpRuleAsync(string userId, XpEventType eventType, CancellationToken cancellationToken = default) => Task.FromResult(XpRules.FirstOrDefault(x => x.UserId == userId && x.EventType == eventType));
        public Task<IReadOnlyCollection<XpLedgerEntry>> GetXpEntriesForSourceAsync(string userId, string sourceType, Guid sourceId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<XpLedgerEntry>>(XpEntries.Where(x => x.UserId == userId && x.SourceType == sourceType && x.SourceId == sourceId).ToArray());
        public Task<IReadOnlyCollection<XpLedgerEntry>> GetXpEntriesAsync(string userId, string sourceType, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<XpLedgerEntry>>(XpEntries.Where(x => x.UserId == userId && x.SourceType == sourceType).ToArray());
        public Task<IReadOnlyCollection<Badge>> GetBadgesAsync(string userId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<Badge>>(Badges.Where(x => x.UserId == userId).ToArray());
        public Task<IReadOnlyCollection<BadgeCriterion>> GetBadgeCriteriaAsync(IReadOnlyCollection<Guid> badgeIds, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<BadgeCriterion>>(BadgeCriteria.Where(x => badgeIds.Contains(x.BadgeId)).ToArray());
        public Task<IReadOnlyCollection<UserBadge>> GetUserBadgesAsync(string userId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<UserBadge>>(UserBadges.Where(x => x.UserId == userId).ToArray());
        public Task AddAsync<T>(T entity, CancellationToken cancellationToken = default) where T : class { Add(entity); return Task.CompletedTask; }
        public Task AddRangeAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default) where T : class { foreach (var entity in entities) Add(entity); return Task.CompletedTask; }
        public Task RemoveAsync<T>(T entity, CancellationToken cancellationToken = default) where T : class { if (entity is HabitSchedule schedule) Schedules.Remove(schedule); if (entity is HabitScheduleWeekday weekday) Weekdays.Remove(weekday); if (entity is UserBadge badge) UserBadges.Remove(badge); return Task.CompletedTask; }
        private void Add<T>(T entity) where T : class
        {
            switch (entity)
            {
                case Habit habit: Habits.Add(habit); break;
                case HabitSchedule schedule: Schedules.Add(schedule); break;
                case HabitScheduleWeekday weekday: Weekdays.Add(weekday); break;
                case HabitCompletion completion: Completions.Add(completion); break;
                case XpLedgerEntry entry: XpEntries.Add(entry); break;
                case UserBadge badge: UserBadges.Add(badge); break;
                default: throw new InvalidOperationException("Unsupported entity.");
            }
        }
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
