using Microsoft.EntityFrameworkCore;
using ServiceLifeOS.Application.Ports;
using ServiceLifeOS.Domain.Entities;

namespace ServiceLifeOS.Infrastructure.Persistence.Repositories;

public sealed class GamificationRepository : IGamificationRepository
{
    private readonly AppDbContext _db;

    public GamificationRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<IReadOnlyCollection<Goal>> GetGoalsAsync(string userId, CancellationToken cancellationToken = default) => GetAsync(_db.Goals.Where(x => x.UserId == userId), cancellationToken);
    public Task<Goal?> GetGoalAsync(string userId, Guid goalId, CancellationToken cancellationToken = default) => _db.Goals.FirstOrDefaultAsync(x => x.UserId == userId && x.Id == goalId, cancellationToken);
    public Task<IReadOnlyCollection<GoalSourceLink>> GetGoalSourcesAsync(IReadOnlyCollection<Guid> goalIds, CancellationToken cancellationToken = default) => GetAsync(_db.GoalSourceLinks.Where(x => goalIds.Contains(x.GoalId)), cancellationToken);
    public Task<IReadOnlyCollection<XpEventRule>> GetXpRulesAsync(string userId, CancellationToken cancellationToken = default) => GetAsync(_db.XpEventRules.Where(x => x.UserId == userId), cancellationToken);
    public Task<LevelProgressionRule?> GetLevelRuleAsync(string userId, CancellationToken cancellationToken = default) => _db.LevelProgressionRules.FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
    public Task<IReadOnlyCollection<Badge>> GetBadgesAsync(string userId, CancellationToken cancellationToken = default) => GetAsync(_db.Badges.Where(x => x.UserId == userId), cancellationToken);
    public Task<IReadOnlyCollection<BadgeCriterion>> GetBadgeCriteriaAsync(IReadOnlyCollection<Guid> badgeIds, CancellationToken cancellationToken = default) => GetAsync(_db.BadgeCriteria.Where(x => badgeIds.Contains(x.BadgeId)), cancellationToken);
    public Task<IReadOnlyCollection<UserBadge>> GetUserBadgesAsync(string userId, CancellationToken cancellationToken = default) => GetAsync(_db.UserBadges.Where(x => x.UserId == userId), cancellationToken);
    public Task<IReadOnlyCollection<XpLedgerEntry>> GetXpEntriesAsync(string userId, CancellationToken cancellationToken = default) => GetAsync(_db.XpLedgerEntries.Where(x => x.UserId == userId), cancellationToken);
    public Task<IReadOnlyCollection<FinancialTransaction>> GetTransactionsAsync(string userId, CancellationToken cancellationToken = default) => GetAsync(_db.FinancialTransactions.Where(x => x.UserId == userId), cancellationToken);
    public Task<IReadOnlyCollection<FinancialCategory>> GetCategoriesAsync(string userId, CancellationToken cancellationToken = default) => GetAsync(_db.FinancialCategories.Where(x => x.UserId == userId), cancellationToken);
    public Task<IReadOnlyCollection<CategoryBudget>> GetBudgetsAsync(IReadOnlyCollection<Guid> categoryIds, CancellationToken cancellationToken = default) => GetAsync(_db.CategoryBudgets.Where(x => categoryIds.Contains(x.CategoryId)), cancellationToken);
    public Task<IReadOnlyCollection<CategoryBudgetOverride>> GetBudgetOverridesAsync(IReadOnlyCollection<Guid> budgetIds, DateOnly month, CancellationToken cancellationToken = default) => GetAsync(_db.CategoryBudgetOverrides.Where(x => budgetIds.Contains(x.CategoryBudgetId) && x.Month == month), cancellationToken);
    public Task<IReadOnlyCollection<Habit>> GetHabitsAsync(string userId, CancellationToken cancellationToken = default) => GetAsync(_db.Habits.Where(x => x.UserId == userId), cancellationToken);
    public Task<IReadOnlyCollection<HabitSchedule>> GetHabitSchedulesAsync(IReadOnlyCollection<Guid> habitIds, CancellationToken cancellationToken = default) => GetAsync(_db.HabitSchedules.Where(x => habitIds.Contains(x.HabitId)), cancellationToken);
    public Task<IReadOnlyCollection<HabitScheduleWeekday>> GetHabitWeekdaysAsync(IReadOnlyCollection<Guid> scheduleIds, CancellationToken cancellationToken = default) => GetAsync(_db.HabitScheduleWeekdays.Where(x => scheduleIds.Contains(x.HabitScheduleId)), cancellationToken);
    public Task<IReadOnlyCollection<HabitCompletion>> GetHabitCompletionsAsync(string userId, CancellationToken cancellationToken = default) => GetAsync(_db.HabitCompletions.Where(x => x.UserId == userId), cancellationToken);
    public Task<IReadOnlyCollection<WorkoutSession>> GetWorkoutSessionsAsync(string userId, CancellationToken cancellationToken = default) => GetAsync(_db.WorkoutSessions.Where(x => x.UserId == userId), cancellationToken);
    public Task<IReadOnlyCollection<WorkoutSessionExercise>> GetWorkoutSessionExercisesAsync(IReadOnlyCollection<Guid> sessionIds, CancellationToken cancellationToken = default) => GetAsync(_db.WorkoutSessionExercises.Where(x => sessionIds.Contains(x.WorkoutSessionId)), cancellationToken);
    public Task<IReadOnlyCollection<WorkoutSheet>> GetWorkoutSheetsAsync(string userId, CancellationToken cancellationToken = default) => GetAsync(_db.WorkoutSheets.Where(x => x.UserId == userId), cancellationToken);
    public Task<IReadOnlyCollection<Exercise>> GetExercisesAsync(string userId, CancellationToken cancellationToken = default) => GetAsync(_db.Exercises.Where(x => x.UserId == userId), cancellationToken);

    public Task AddAsync<T>(T entity, CancellationToken cancellationToken = default) where T : class { _db.Set<T>().Add(entity); return Task.CompletedTask; }
    public Task AddRangeAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default) where T : class { _db.Set<T>().AddRange(entities); return Task.CompletedTask; }
    public Task RemoveAsync<T>(T entity, CancellationToken cancellationToken = default) where T : class { _db.Set<T>().Remove(entity); return Task.CompletedTask; }

    private static async Task<IReadOnlyCollection<T>> GetAsync<T>(IQueryable<T> query, CancellationToken cancellationToken) where T : class => await query.ToArrayAsync(cancellationToken);
}
