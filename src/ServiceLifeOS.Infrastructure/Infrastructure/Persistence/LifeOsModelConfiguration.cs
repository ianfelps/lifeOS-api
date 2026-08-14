using Microsoft.EntityFrameworkCore;
using ServiceLifeOS.Domain.Entities;

namespace ServiceLifeOS.Infrastructure.Persistence;

internal static class LifeOsModelConfiguration
{
    public static void ConfigureLifeOsEntities(this ModelBuilder modelBuilder)
    {
        Configure(modelBuilder.Entity<UserPreference>(), "user_preferences", x => x.UserId);
        Configure(modelBuilder.Entity<UserSession>(), "user_sessions", x => x.UserId);
        Configure(modelBuilder.Entity<AuditLog>(), "audit_logs", x => x.UserId);
        Configure(modelBuilder.Entity<FinancialCategory>(), "financial_categories", x => x.UserId);
        Configure(modelBuilder.Entity<CategoryBudget>(), "category_budgets", x => x.CategoryId);
        Configure(
            modelBuilder.Entity<CategoryBudgetOverride>(),
            "category_budget_overrides",
            x => x.CategoryBudgetId);
        Configure(modelBuilder.Entity<RecurringTransaction>(), "recurring_transactions", x => x.UserId);
        Configure(modelBuilder.Entity<InstallmentPurchase>(), "installment_purchases", x => x.UserId);
        Configure(modelBuilder.Entity<FinancialTransaction>(), "financial_transactions", x => x.UserId);
        Configure(modelBuilder.Entity<Habit>(), "habits", x => x.UserId);
        Configure(modelBuilder.Entity<HabitSchedule>(), "habit_schedules", x => x.HabitId);
        Configure(
            modelBuilder.Entity<HabitScheduleWeekday>(),
            "habit_schedule_weekdays",
            x => x.HabitScheduleId);
        Configure(modelBuilder.Entity<HabitCompletion>(), "habit_completions", x => x.UserId);
        Configure(modelBuilder.Entity<Exercise>(), "exercises", x => x.UserId);
        Configure(modelBuilder.Entity<WorkoutSheet>(), "workout_sheets", x => x.UserId);
        Configure(
            modelBuilder.Entity<WorkoutSheetExercise>(),
            "workout_sheet_exercises",
            x => x.WorkoutSheetId);
        Configure(
            modelBuilder.Entity<WorkoutSheetExerciseSet>(),
            "workout_sheet_exercise_sets",
            x => x.WorkoutSheetExerciseId);
        Configure(modelBuilder.Entity<WorkoutSession>(), "workout_sessions", x => x.UserId);
        Configure(
            modelBuilder.Entity<WorkoutSessionExercise>(),
            "workout_session_exercises",
            x => x.WorkoutSessionId);
        Configure(
            modelBuilder.Entity<WorkoutSessionSet>(),
            "workout_session_sets",
            x => x.WorkoutSessionExerciseId);
        Configure(modelBuilder.Entity<Goal>(), "goals", x => x.UserId);
        Configure(modelBuilder.Entity<GoalSourceLink>(), "goal_source_links", x => x.GoalId);
        Configure(modelBuilder.Entity<XpEventRule>(), "xp_event_rules", x => x.UserId);
        Configure(modelBuilder.Entity<LevelProgressionRule>(), "level_progression_rules", x => x.UserId);
        Configure(modelBuilder.Entity<Badge>(), "badges", x => x.UserId);
        Configure(modelBuilder.Entity<BadgeCriterion>(), "badge_criteria", x => x.BadgeId);
        Configure(modelBuilder.Entity<UserBadge>(), "user_badges", x => x.UserId);
        Configure(modelBuilder.Entity<XpLedgerEntry>(), "xp_ledger_entries", x => x.UserId);

        modelBuilder.Entity<UserPreference>().HasIndex(x => x.UserId).IsUnique();
        modelBuilder.Entity<UserSession>().HasIndex(x => x.TokenId).IsUnique();
        modelBuilder.Entity<FinancialCategory>()
            .HasIndex(x => new { x.UserId, x.Name, x.Type })
            .IsUnique();
        modelBuilder.Entity<CategoryBudget>().HasIndex(x => x.CategoryId).IsUnique();
        modelBuilder.Entity<CategoryBudgetOverride>()
            .HasIndex(x => new { x.CategoryBudgetId, x.Month })
            .IsUnique();
        modelBuilder.Entity<XpEventRule>()
            .HasIndex(x => new { x.UserId, x.EventType })
            .IsUnique();
        modelBuilder.Entity<LevelProgressionRule>().HasIndex(x => x.UserId).IsUnique();
        modelBuilder.Entity<UserBadge>()
            .HasIndex(x => new { x.UserId, x.BadgeId })
            .IsUnique();
        modelBuilder.Entity<WorkoutSheetExercise>()
            .HasIndex(x => new { x.WorkoutSheetId, x.Position })
            .IsUnique();
        modelBuilder.Entity<WorkoutSheetExerciseSet>()
            .HasIndex(x => new { x.WorkoutSheetExerciseId, x.Position })
            .IsUnique();
        modelBuilder.Entity<WorkoutSessionExercise>()
            .HasIndex(x => new { x.WorkoutSessionId, x.Position })
            .IsUnique();
        modelBuilder.Entity<WorkoutSessionSet>()
            .HasIndex(x => new { x.WorkoutSessionExerciseId, x.Position })
            .IsUnique();

        modelBuilder.Entity<UserPreference>()
            .HasOne<AppUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<UserSession>()
            .HasOne<AppUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<AuditLog>()
            .HasOne<AppUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<FinancialCategory>()
            .HasOne<AppUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<RecurringTransaction>()
            .HasOne<AppUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<InstallmentPurchase>()
            .HasOne<AppUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<FinancialTransaction>()
            .HasOne<AppUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Habit>()
            .HasOne<AppUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<HabitCompletion>()
            .HasOne<AppUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Exercise>()
            .HasOne<AppUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<WorkoutSheet>()
            .HasOne<AppUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<WorkoutSession>()
            .HasOne<AppUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Goal>()
            .HasOne<AppUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<XpEventRule>()
            .HasOne<AppUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<LevelProgressionRule>()
            .HasOne<AppUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Badge>()
            .HasOne<AppUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<UserBadge>()
            .HasOne<AppUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<XpLedgerEntry>()
            .HasOne<AppUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<CategoryBudget>()
            .HasOne<FinancialCategory>().WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<CategoryBudgetOverride>()
            .HasOne<CategoryBudget>().WithMany()
            .HasForeignKey(x => x.CategoryBudgetId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<RecurringTransaction>()
            .HasOne<FinancialCategory>().WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<InstallmentPurchase>()
            .HasOne<FinancialCategory>().WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<FinancialTransaction>()
            .HasOne<FinancialCategory>().WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<FinancialTransaction>()
            .HasOne<RecurringTransaction>().WithMany()
            .HasForeignKey(x => x.RecurringTransactionId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<FinancialTransaction>()
            .HasOne<InstallmentPurchase>().WithMany()
            .HasForeignKey(x => x.InstallmentPurchaseId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<HabitSchedule>()
            .HasOne<Habit>().WithMany().HasForeignKey(x => x.HabitId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<HabitScheduleWeekday>()
            .HasOne<HabitSchedule>().WithMany().HasForeignKey(x => x.HabitScheduleId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<HabitCompletion>()
            .HasOne<Habit>().WithMany().HasForeignKey(x => x.HabitId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<WorkoutSheetExercise>()
            .HasOne<WorkoutSheet>().WithMany().HasForeignKey(x => x.WorkoutSheetId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<WorkoutSheetExercise>()
            .HasOne<Exercise>().WithMany().HasForeignKey(x => x.ExerciseId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<WorkoutSheetExerciseSet>()
            .HasOne<WorkoutSheetExercise>().WithMany()
            .HasForeignKey(x => x.WorkoutSheetExerciseId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<WorkoutSession>()
            .HasOne<WorkoutSheet>().WithMany().HasForeignKey(x => x.WorkoutSheetId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<WorkoutSessionExercise>()
            .HasOne<WorkoutSession>().WithMany()
            .HasForeignKey(x => x.WorkoutSessionId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<WorkoutSessionExercise>()
            .HasOne<Exercise>().WithMany().HasForeignKey(x => x.ExerciseId).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<WorkoutSessionSet>()
            .HasOne<WorkoutSessionExercise>().WithMany()
            .HasForeignKey(x => x.WorkoutSessionExerciseId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<GoalSourceLink>()
            .HasOne<Goal>().WithMany().HasForeignKey(x => x.GoalId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<BadgeCriterion>()
            .HasOne<Badge>().WithMany().HasForeignKey(x => x.BadgeId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<BadgeCriterion>()
            .HasOne<Habit>().WithMany().HasForeignKey(x => x.HabitId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<BadgeCriterion>()
            .HasOne<Exercise>().WithMany().HasForeignKey(x => x.ExerciseId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<BadgeCriterion>()
            .HasOne<FinancialCategory>().WithMany()
            .HasForeignKey(x => x.FinancialCategoryId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<BadgeCriterion>()
            .HasOne<Goal>().WithMany().HasForeignKey(x => x.GoalId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<UserBadge>()
            .HasOne<Badge>().WithMany().HasForeignKey(x => x.BadgeId).OnDelete(DeleteBehavior.Cascade);
    }

    private static void Configure<TEntity>(
        Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<TEntity> builder,
        string tableName,
        System.Linq.Expressions.Expression<Func<TEntity, object?>> indexedProperty)
        where TEntity : class
    {
        builder.ToTable(tableName);
        builder.HasKey("Id");
        builder.HasIndex(indexedProperty);
        foreach (var property in builder.Metadata
                     .GetProperties()
                     .Where(x => x.ClrType == typeof(decimal) || x.ClrType == typeof(decimal?)))
        {
            property.SetPrecision(18);
            property.SetScale(2);
        }

        foreach (var property in builder.Metadata.GetProperties())
        {
            property.SetColumnName(ToSnakeCase(property.Name));
        }
    }

    private static string ToSnakeCase(string value)
    {
        return string.Concat(value.Select((character, index) =>
            index > 0 && char.IsUpper(character)
                ? $"_{char.ToLowerInvariant(character)}"
                : char.ToLowerInvariant(character).ToString()));
    }
}
