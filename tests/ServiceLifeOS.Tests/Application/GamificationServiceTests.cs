using ServiceLifeOS.Application.Ports;
using ServiceLifeOS.Application.Services;
using ServiceLifeOS.Domain.Entities;
using ServiceLifeOS.Dtos.Gamification;
using Xunit;

namespace ServiceLifeOS.Tests.Application;

public sealed class GamificationServiceTests
{
    [Fact]
    public async Task ManualGoal_GrantsThenReversesXpAndBadge()
    {
        var repository = new FakeGamificationRepository();
        repository.Rules.Add(new() { UserId = "user-1", EventType = XpEventType.GoalCompleted, Amount = 40 });
        repository.LevelRule = new() { UserId = "user-1", BaseXp = 40, IncrementPerLevel = 10 };
        var badge = new Badge { UserId = "user-1", Name = "Level two", Description = "Reach level two." };
        repository.Badges.Add(badge);
        repository.Criteria.Add(new() { BadgeId = badge.Id, Type = BadgeCriterionType.Level, TargetValue = 2 });
        var service = new GamificationService(repository, new FakeUnitOfWork());

        var goal = await service.CreateGoalAsync("user-1", new() { Type = GoalType.FreeForm, Title = "Read", TargetValue = 10, Unit = "pages" });
        await service.UpdateManualProgressAsync("user-1", goal.Id, new() { Progress = 10 });

        var profile = await service.GetProfileAsync("user-1");
        Assert.Equal(40, profile.TotalXp);
        Assert.Equal(2, profile.Level);
        Assert.NotNull(profile.Badges.Single().UnlockedAt);

        await service.UpdateManualProgressAsync("user-1", goal.Id, new() { Progress = 9 });

        profile = await service.GetProfileAsync("user-1");
        Assert.Equal(0, profile.TotalXp);
        Assert.Equal(1, profile.Level);
        Assert.Null(profile.Badges.Single().UnlockedAt);
    }

    private sealed class FakeGamificationRepository : IGamificationRepository
    {
        public List<Goal> Goals { get; } = [];
        public List<GoalSourceLink> Sources { get; } = [];
        public List<XpEventRule> Rules { get; } = [];
        public LevelProgressionRule? LevelRule { get; set; }
        public List<Badge> Badges { get; } = [];
        public List<BadgeCriterion> Criteria { get; } = [];
        public List<UserBadge> UserBadges { get; } = [];
        public List<XpLedgerEntry> Entries { get; } = [];
        public Task<IReadOnlyCollection<Goal>> GetGoalsAsync(string userId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<Goal>>(Goals.Where(x => x.UserId == userId).ToArray());
        public Task<Goal?> GetGoalAsync(string userId, Guid goalId, CancellationToken cancellationToken = default) => Task.FromResult(Goals.FirstOrDefault(x => x.UserId == userId && x.Id == goalId));
        public Task<IReadOnlyCollection<GoalSourceLink>> GetGoalSourcesAsync(IReadOnlyCollection<Guid> goalIds, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<GoalSourceLink>>(Sources.Where(x => goalIds.Contains(x.GoalId)).ToArray());
        public Task<IReadOnlyCollection<XpEventRule>> GetXpRulesAsync(string userId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<XpEventRule>>(Rules.Where(x => x.UserId == userId).ToArray());
        public Task<LevelProgressionRule?> GetLevelRuleAsync(string userId, CancellationToken cancellationToken = default) => Task.FromResult(LevelRule);
        public Task<IReadOnlyCollection<Badge>> GetBadgesAsync(string userId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<Badge>>(Badges.Where(x => x.UserId == userId).ToArray());
        public Task<IReadOnlyCollection<BadgeCriterion>> GetBadgeCriteriaAsync(IReadOnlyCollection<Guid> badgeIds, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<BadgeCriterion>>(Criteria.Where(x => badgeIds.Contains(x.BadgeId)).ToArray());
        public Task<IReadOnlyCollection<UserBadge>> GetUserBadgesAsync(string userId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<UserBadge>>(UserBadges.Where(x => x.UserId == userId).ToArray());
        public Task<IReadOnlyCollection<XpLedgerEntry>> GetXpEntriesAsync(string userId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<XpLedgerEntry>>(Entries.Where(x => x.UserId == userId).ToArray());
        public Task<IReadOnlyCollection<FinancialTransaction>> GetTransactionsAsync(string userId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<FinancialTransaction>>([]);
        public Task<IReadOnlyCollection<FinancialCategory>> GetCategoriesAsync(string userId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<FinancialCategory>>([]);
        public Task<IReadOnlyCollection<CategoryBudget>> GetBudgetsAsync(IReadOnlyCollection<Guid> categoryIds, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<CategoryBudget>>([]);
        public Task<IReadOnlyCollection<CategoryBudgetOverride>> GetBudgetOverridesAsync(IReadOnlyCollection<Guid> budgetIds, DateOnly month, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<CategoryBudgetOverride>>([]);
        public Task<IReadOnlyCollection<Habit>> GetHabitsAsync(string userId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<Habit>>([]);
        public Task<IReadOnlyCollection<HabitSchedule>> GetHabitSchedulesAsync(IReadOnlyCollection<Guid> habitIds, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<HabitSchedule>>([]);
        public Task<IReadOnlyCollection<HabitScheduleWeekday>> GetHabitWeekdaysAsync(IReadOnlyCollection<Guid> scheduleIds, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<HabitScheduleWeekday>>([]);
        public Task<IReadOnlyCollection<HabitCompletion>> GetHabitCompletionsAsync(string userId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<HabitCompletion>>([]);
        public Task<IReadOnlyCollection<WorkoutSession>> GetWorkoutSessionsAsync(string userId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<WorkoutSession>>([]);
        public Task<IReadOnlyCollection<WorkoutSessionExercise>> GetWorkoutSessionExercisesAsync(IReadOnlyCollection<Guid> sessionIds, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<WorkoutSessionExercise>>([]);
        public Task<IReadOnlyCollection<WorkoutSheet>> GetWorkoutSheetsAsync(string userId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<WorkoutSheet>>([]);
        public Task<IReadOnlyCollection<Exercise>> GetExercisesAsync(string userId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<Exercise>>([]);
        public Task AddAsync<T>(T entity, CancellationToken cancellationToken = default) where T : class { Add(entity); return Task.CompletedTask; }
        public Task AddRangeAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default) where T : class { foreach (var entity in entities) Add(entity); return Task.CompletedTask; }
        public Task RemoveAsync<T>(T entity, CancellationToken cancellationToken = default) where T : class { if (entity is UserBadge badge) UserBadges.Remove(badge); if (entity is GoalSourceLink source) Sources.Remove(source); if (entity is BadgeCriterion criterion) Criteria.Remove(criterion); return Task.CompletedTask; }
        private void Add<T>(T entity) where T : class { switch (entity) { case Goal value: Goals.Add(value); break; case GoalSourceLink value: Sources.Add(value); break; case XpLedgerEntry value: Entries.Add(value); break; case UserBadge value: UserBadges.Add(value); break; case XpEventRule value: Rules.Add(value); break; case Badge value: Badges.Add(value); break; case BadgeCriterion value: Criteria.Add(value); break; default: throw new InvalidOperationException("Unsupported entity."); } }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
        public Task<IAppTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
