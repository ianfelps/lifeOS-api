using ServiceLifeOS.Domain.Entities;

namespace ServiceLifeOS.Application.Ports;

public interface IFinanceRepository
{
    Task<IReadOnlyCollection<FinancialCategory>> GetCategoriesAsync(string userId, bool includeArchived, CancellationToken cancellationToken = default);
    Task<FinancialCategory?> GetCategoryAsync(string userId, Guid categoryId, CancellationToken cancellationToken = default);
    Task<FinancialTransaction?> GetTransactionAsync(string userId, Guid transactionId, CancellationToken cancellationToken = default);
    Task<RecurringTransaction?> GetRecurringTransactionAsync(string userId, Guid recurringTransactionId, CancellationToken cancellationToken = default);
    Task<InstallmentPurchase?> GetInstallmentPurchaseAsync(string userId, Guid installmentPurchaseId, CancellationToken cancellationToken = default);
    Task<CategoryBudget?> GetBudgetAsync(Guid categoryId, CancellationToken cancellationToken = default);
    Task<CategoryBudgetOverride?> GetBudgetOverrideAsync(Guid budgetId, DateOnly month, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<FinancialTransaction>> GetTransactionsAsync(string userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<FinancialTransaction>> GetTransactionsForInstallmentPurchaseAsync(string userId, Guid installmentPurchaseId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<RecurringTransaction>> GetRecurringTransactionsAsync(string userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<CategoryBudget>> GetBudgetsAsync(IReadOnlyCollection<Guid> categoryIds, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<CategoryBudgetOverride>> GetBudgetOverridesAsync(IReadOnlyCollection<Guid> budgetIds, DateOnly month, CancellationToken cancellationToken = default);
    Task<XpEventRule?> GetXpRuleAsync(string userId, XpEventType eventType, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<XpLedgerEntry>> GetXpEntriesForSourceAsync(string userId, string sourceType, Guid sourceId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Badge>> GetBadgesAsync(string userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<BadgeCriterion>> GetBadgeCriteriaAsync(IReadOnlyCollection<Guid> badgeIds, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<UserBadge>> GetUserBadgesAsync(string userId, CancellationToken cancellationToken = default);
    Task AddAsync<T>(T entity, CancellationToken cancellationToken = default) where T : class;
    Task AddRangeAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default) where T : class;
    Task RemoveAsync<T>(T entity, CancellationToken cancellationToken = default) where T : class;
}
