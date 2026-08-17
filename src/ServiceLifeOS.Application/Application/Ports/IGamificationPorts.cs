using ServiceLifeOS.Domain.Entities;

namespace ServiceLifeOS.Application.Ports;

public interface IGamificationRepository
{
    Task<IReadOnlyCollection<Goal>> GetGoalsAsync(string userId, CancellationToken cancellationToken = default);
    Task<Goal?> GetGoalAsync(string userId, Guid goalId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<GoalSourceLink>> GetGoalSourcesAsync(IReadOnlyCollection<Guid> goalIds, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<XpEventRule>> GetXpRulesAsync(string userId, CancellationToken cancellationToken = default);
    Task<LevelProgressionRule?> GetLevelRuleAsync(string userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Badge>> GetBadgesAsync(string userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<BadgeCriterion>> GetBadgeCriteriaAsync(IReadOnlyCollection<Guid> badgeIds, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<UserBadge>> GetUserBadgesAsync(string userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<XpLedgerEntry>> GetXpEntriesAsync(string userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<FinancialTransaction>> GetTransactionsAsync(string userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<FinancialCategory>> GetCategoriesAsync(string userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<CategoryBudget>> GetBudgetsAsync(IReadOnlyCollection<Guid> categoryIds, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<CategoryBudgetOverride>> GetBudgetOverridesAsync(IReadOnlyCollection<Guid> budgetIds, DateOnly month, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Habit>> GetHabitsAsync(string userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<HabitSchedule>> GetHabitSchedulesAsync(IReadOnlyCollection<Guid> habitIds, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<HabitScheduleWeekday>> GetHabitWeekdaysAsync(IReadOnlyCollection<Guid> scheduleIds, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<HabitCompletion>> GetHabitCompletionsAsync(string userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<WorkoutSession>> GetWorkoutSessionsAsync(string userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<WorkoutSessionExercise>> GetWorkoutSessionExercisesAsync(IReadOnlyCollection<Guid> sessionIds, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<WorkoutSheet>> GetWorkoutSheetsAsync(string userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Exercise>> GetExercisesAsync(string userId, CancellationToken cancellationToken = default);
    Task AddAsync<T>(T entity, CancellationToken cancellationToken = default) where T : class;
    Task AddRangeAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default) where T : class;
    Task RemoveAsync<T>(T entity, CancellationToken cancellationToken = default) where T : class;
}
