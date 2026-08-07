using Microsoft.EntityFrameworkCore;
using ServiceLifeOS.Application.Ports;
using ServiceLifeOS.Domain.Entities;
using ServiceLifeOS.Infrastructure.Persistence.Repositories;

namespace ServiceLifeOS.Infrastructure.Persistence;

public sealed class AppDbContext : DbContext, IUnitOfWork
{
    public AppDbContext(
        DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<UserPreference> UserPreferences => Set<UserPreference>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<FinancialCategory> FinancialCategories => Set<FinancialCategory>();
    public DbSet<CategoryBudget> CategoryBudgets => Set<CategoryBudget>();
    public DbSet<CategoryBudgetOverride> CategoryBudgetOverrides => Set<CategoryBudgetOverride>();
    public DbSet<RecurringTransaction> RecurringTransactions => Set<RecurringTransaction>();
    public DbSet<InstallmentPurchase> InstallmentPurchases => Set<InstallmentPurchase>();
    public DbSet<FinancialTransaction> FinancialTransactions => Set<FinancialTransaction>();
    public DbSet<Habit> Habits => Set<Habit>();
    public DbSet<HabitSchedule> HabitSchedules => Set<HabitSchedule>();
    public DbSet<HabitScheduleWeekday> HabitScheduleWeekdays => Set<HabitScheduleWeekday>();
    public DbSet<HabitCompletion> HabitCompletions => Set<HabitCompletion>();
    public DbSet<Exercise> Exercises => Set<Exercise>();
    public DbSet<WorkoutSheet> WorkoutSheets => Set<WorkoutSheet>();
    public DbSet<WorkoutSheetExercise> WorkoutSheetExercises => Set<WorkoutSheetExercise>();
    public DbSet<WorkoutSheetExerciseSet> WorkoutSheetExerciseSets => Set<WorkoutSheetExerciseSet>();
    public DbSet<WorkoutSession> WorkoutSessions => Set<WorkoutSession>();
    public DbSet<WorkoutSessionExercise> WorkoutSessionExercises => Set<WorkoutSessionExercise>();
    public DbSet<WorkoutSessionSet> WorkoutSessionSets => Set<WorkoutSessionSet>();
    public DbSet<Goal> Goals => Set<Goal>();
    public DbSet<GoalSourceLink> GoalSourceLinks => Set<GoalSourceLink>();
    public DbSet<XpEventRule> XpEventRules => Set<XpEventRule>();
    public DbSet<LevelProgressionRule> LevelProgressionRules => Set<LevelProgressionRule>();
    public DbSet<Badge> Badges => Set<Badge>();
    public DbSet<BadgeCriterion> BadgeCriteria => Set<BadgeCriterion>();
    public DbSet<UserBadge> UserBadges => Set<UserBadge>();
    public DbSet<XpLedgerEntry> XpLedgerEntries => Set<XpLedgerEntry>();

    public async Task<IAppTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        var transaction = await Database.BeginTransactionAsync(cancellationToken);

        return new EfAppTransaction(transaction);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        modelBuilder.ConfigureLifeOsEntities();
    }
}
