using Microsoft.EntityFrameworkCore;
using ServiceLifeOS.Application.Options;
using ServiceLifeOS.Application.Ports;
using ServiceLifeOS.Domain.Entities;

namespace ServiceLifeOS.Infrastructure.Persistence;

public static class DbSeeder
{
    public static async Task SeedAsync(
        AppDbContext db,
        BootstrapUserOptions bootstrapUser,
        IPasswordHasher passwordHasher,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var user = await db.Users.FirstOrDefaultAsync(
            x => x.Id == bootstrapUser.UserId,
            cancellationToken);

        if (user is null)
        {
            db.Users.Add(new AppUser
            {
                Id = bootstrapUser.UserId.Trim(),
                UserName = bootstrapUser.UserName.Trim(),
                DisplayName = bootstrapUser.DisplayName.Trim(),
                PasswordHash = passwordHasher.HashPassword(bootstrapUser.Password),
                Active = true,
                CreatedAt = now,
                UpdatedAt = now
            });
        }
        else
        {
            user.UserName = bootstrapUser.UserName.Trim();
            user.DisplayName = bootstrapUser.DisplayName.Trim();
            user.Active = true;
            user.UpdatedAt = now;

            if (string.IsNullOrWhiteSpace(user.PasswordHash))
            {
                user.PasswordHash = passwordHasher.HashPassword(bootstrapUser.Password);
            }
        }

        await SeedDefaultsAsync(db, bootstrapUser.UserId, now, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedDefaultsAsync(
        AppDbContext db,
        string userId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (!await db.UserPreferences.AnyAsync(x => x.UserId == userId, cancellationToken))
        {
            db.UserPreferences.Add(new UserPreference { UserId = userId, CreatedAt = now, UpdatedAt = now });
        }

        if (!await db.FinancialCategories.AnyAsync(x => x.UserId == userId, cancellationToken))
        {
            var categories = new (string Name, FinancialCategoryType Type)[]
            {
                ("Salario", FinancialCategoryType.Income), ("Freelance", FinancialCategoryType.Income), ("Investimentos", FinancialCategoryType.Income), ("Reembolsos", FinancialCategoryType.Income), ("Outras receitas", FinancialCategoryType.Income),
                ("Moradia", FinancialCategoryType.Expense), ("Alimentacao", FinancialCategoryType.Expense), ("Transporte", FinancialCategoryType.Expense), ("Saude", FinancialCategoryType.Expense), ("Educacao", FinancialCategoryType.Expense), ("Lazer", FinancialCategoryType.Expense), ("Assinaturas", FinancialCategoryType.Expense), ("Cuidados pessoais", FinancialCategoryType.Expense), ("Compras", FinancialCategoryType.Expense), ("Impostos e taxas", FinancialCategoryType.Expense), ("Outras despesas", FinancialCategoryType.Expense)
            };
            db.FinancialCategories.AddRange(categories.Select(x => new FinancialCategory { UserId = userId, Name = x.Name, Type = x.Type, CreatedAt = now, UpdatedAt = now }));
        }

        if (!await db.XpEventRules.AnyAsync(x => x.UserId == userId, cancellationToken))
        {
            var rules = new (XpEventType EventType, int Amount)[]
            {
                (XpEventType.HabitCompletion, 5), (XpEventType.WeeklyHabitGoal, 20), (XpEventType.WorkoutCompleted, 25), (XpEventType.TransactionConfirmed, 1), (XpEventType.PositiveMonth, 50), (XpEventType.GoalCompleted, 40)
            };
            db.XpEventRules.AddRange(rules.Select(x => new XpEventRule { UserId = userId, EventType = x.EventType, Amount = x.Amount, CreatedAt = now, UpdatedAt = now }));
        }

        if (!await db.LevelProgressionRules.AnyAsync(x => x.UserId == userId, cancellationToken))
        {
            db.LevelProgressionRules.Add(new LevelProgressionRule { UserId = userId, BaseXp = 100, IncrementPerLevel = 25, CreatedAt = now, UpdatedAt = now });
        }

        if (!await db.Badges.AnyAsync(x => x.UserId == userId, cancellationToken))
        {
            AddBadge(db, userId, "Primeiro passo", "Conclua 1 habito.", BadgeCriterionType.HabitCompletionCount, 1, now);
            AddBadge(db, userId, "Habitos em dezena", "Conclua 10 habitos.", BadgeCriterionType.HabitCompletionCount, 10, now);
            AddBadge(db, userId, "Habitos em centena", "Conclua 100 habitos.", BadgeCriterionType.HabitCompletionCount, 100, now);
            AddBadge(db, userId, "Ritmo semanal", "Cumpra 4 metas semanais de habito.", BadgeCriterionType.WeeklyHabitGoalCount, 4, now);
            AddBadge(db, userId, "Semanas em dezena", "Cumpra 10 metas semanais de habito.", BadgeCriterionType.WeeklyHabitGoalCount, 10, now);
            AddBadge(db, userId, "Semanas em centena", "Cumpra 100 metas semanais de habito.", BadgeCriterionType.WeeklyHabitGoalCount, 100, now);
            AddBadge(db, userId, "Primeiro treino", "Conclua 1 treino.", BadgeCriterionType.WorkoutCompletionCount, 1, now);
            AddBadge(db, userId, "Treinos em dezena", "Conclua 10 treinos.", BadgeCriterionType.WorkoutCompletionCount, 10, now);
            AddBadge(db, userId, "Treinos em centena", "Conclua 100 treinos.", BadgeCriterionType.WorkoutCompletionCount, 100, now);
            AddBadge(db, userId, "Transacoes em dezena", "Confirme 10 transacoes.", BadgeCriterionType.TransactionConfirmationCount, 10, now);
            AddBadge(db, userId, "Transacoes registradas", "Confirme 25 transacoes.", BadgeCriterionType.TransactionConfirmationCount, 25, now);
            AddBadge(db, userId, "Transacoes em centena", "Confirme 100 transacoes.", BadgeCriterionType.TransactionConfirmationCount, 100, now);
            AddBadge(db, userId, "Primeira meta", "Conclua 1 meta pessoal.", BadgeCriterionType.GoalCompletionCount, 1, now);
            AddBadge(db, userId, "Metas em dezena", "Conclua 10 metas pessoais.", BadgeCriterionType.GoalCompletionCount, 10, now);
            AddBadge(db, userId, "Metas em centena", "Conclua 100 metas pessoais.", BadgeCriterionType.GoalCompletionCount, 100, now);
            AddBadge(db, userId, "Mes no azul", "Feche 1 mes no azul.", BadgeCriterionType.PositiveMonthCount, 1, now);
            AddBadge(db, userId, "Nivel cinco", "Alcance o nivel 5.", BadgeCriterionType.Level, 5, now);
        }
    }

    private static void AddBadge(AppDbContext db, string userId, string name, string description, BadgeCriterionType type, decimal targetValue, DateTime now)
    {
        var badge = new Badge { UserId = userId, Name = name, Description = description, CreatedAt = now, UpdatedAt = now };
        db.Badges.Add(badge);
        db.BadgeCriteria.Add(new BadgeCriterion { BadgeId = badge.Id, Type = type, TargetValue = targetValue });
    }
}
