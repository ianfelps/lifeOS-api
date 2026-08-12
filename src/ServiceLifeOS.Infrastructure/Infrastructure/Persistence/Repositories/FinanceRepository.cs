using Microsoft.EntityFrameworkCore;
using ServiceLifeOS.Application.Ports;
using ServiceLifeOS.Domain.Entities;

namespace ServiceLifeOS.Infrastructure.Persistence.Repositories;

public sealed class FinanceRepository : IFinanceRepository
{
    private readonly AppDbContext _db;

    public FinanceRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyCollection<FinancialCategory>> GetCategoriesAsync(string userId, bool includeArchived, CancellationToken cancellationToken = default)
    {
        return await _db.FinancialCategories.AsNoTracking().Where(x => x.UserId == userId && (includeArchived || !x.Archived)).OrderBy(x => x.Type).ThenBy(x => x.Name).ToArrayAsync(cancellationToken);
    }

    public Task<FinancialCategory?> GetCategoryAsync(string userId, Guid categoryId, CancellationToken cancellationToken = default) => _db.FinancialCategories.FirstOrDefaultAsync(x => x.UserId == userId && x.Id == categoryId, cancellationToken);
    public Task<FinancialTransaction?> GetTransactionAsync(string userId, Guid transactionId, CancellationToken cancellationToken = default) => _db.FinancialTransactions.FirstOrDefaultAsync(x => x.UserId == userId && x.Id == transactionId && x.DeletedAt == null, cancellationToken);
    public Task<RecurringTransaction?> GetRecurringTransactionAsync(string userId, Guid recurringTransactionId, CancellationToken cancellationToken = default) => _db.RecurringTransactions.FirstOrDefaultAsync(x => x.UserId == userId && x.Id == recurringTransactionId, cancellationToken);
    public Task<InstallmentPurchase?> GetInstallmentPurchaseAsync(string userId, Guid installmentPurchaseId, CancellationToken cancellationToken = default) => _db.InstallmentPurchases.FirstOrDefaultAsync(x => x.UserId == userId && x.Id == installmentPurchaseId, cancellationToken);
    public Task<CategoryBudget?> GetBudgetAsync(Guid categoryId, CancellationToken cancellationToken = default) => _db.CategoryBudgets.FirstOrDefaultAsync(x => x.CategoryId == categoryId, cancellationToken);
    public Task<CategoryBudgetOverride?> GetBudgetOverrideAsync(Guid budgetId, DateOnly month, CancellationToken cancellationToken = default) => _db.CategoryBudgetOverrides.FirstOrDefaultAsync(x => x.CategoryBudgetId == budgetId && x.Month == month, cancellationToken);
    public async Task<IReadOnlyCollection<FinancialTransaction>> GetTransactionsAsync(string userId, CancellationToken cancellationToken = default) => await _db.FinancialTransactions.AsNoTracking().Where(x => x.UserId == userId).ToArrayAsync(cancellationToken);
    public async Task<IReadOnlyCollection<FinancialTransaction>> GetTransactionsForInstallmentPurchaseAsync(string userId, Guid installmentPurchaseId, CancellationToken cancellationToken = default) => await _db.FinancialTransactions.Where(x => x.UserId == userId && x.InstallmentPurchaseId == installmentPurchaseId).ToArrayAsync(cancellationToken);
    public async Task<IReadOnlyCollection<RecurringTransaction>> GetRecurringTransactionsAsync(string userId, CancellationToken cancellationToken = default) => await _db.RecurringTransactions.Where(x => x.UserId == userId).ToArrayAsync(cancellationToken);
    public async Task<IReadOnlyCollection<CategoryBudget>> GetBudgetsAsync(IReadOnlyCollection<Guid> categoryIds, CancellationToken cancellationToken = default) => await _db.CategoryBudgets.AsNoTracking().Where(x => categoryIds.Contains(x.CategoryId)).ToArrayAsync(cancellationToken);
    public async Task<IReadOnlyCollection<CategoryBudgetOverride>> GetBudgetOverridesAsync(IReadOnlyCollection<Guid> budgetIds, DateOnly month, CancellationToken cancellationToken = default) => await _db.CategoryBudgetOverrides.AsNoTracking().Where(x => budgetIds.Contains(x.CategoryBudgetId) && x.Month == month).ToArrayAsync(cancellationToken);
    public Task<XpEventRule?> GetXpRuleAsync(string userId, XpEventType eventType, CancellationToken cancellationToken = default) => _db.XpEventRules.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId && x.EventType == eventType, cancellationToken);
    public async Task<IReadOnlyCollection<XpLedgerEntry>> GetXpEntriesForSourceAsync(string userId, string sourceType, Guid sourceId, CancellationToken cancellationToken = default) => await _db.XpLedgerEntries.AsNoTracking().Where(x => x.UserId == userId && x.SourceType == sourceType && x.SourceId == sourceId).ToArrayAsync(cancellationToken);
    public async Task<IReadOnlyCollection<Badge>> GetBadgesAsync(string userId, CancellationToken cancellationToken = default) => await _db.Badges.AsNoTracking().Where(x => x.UserId == userId && !x.Archived).ToArrayAsync(cancellationToken);
    public async Task<IReadOnlyCollection<BadgeCriterion>> GetBadgeCriteriaAsync(IReadOnlyCollection<Guid> badgeIds, CancellationToken cancellationToken = default) => await _db.BadgeCriteria.AsNoTracking().Where(x => badgeIds.Contains(x.BadgeId)).ToArrayAsync(cancellationToken);
    public async Task<IReadOnlyCollection<UserBadge>> GetUserBadgesAsync(string userId, CancellationToken cancellationToken = default) => await _db.UserBadges.AsNoTracking().Where(x => x.UserId == userId).ToArrayAsync(cancellationToken);
    public Task AddAsync<T>(T entity, CancellationToken cancellationToken = default) where T : class { _db.Set<T>().Add(entity); return Task.CompletedTask; }
    public Task AddRangeAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default) where T : class { _db.Set<T>().AddRange(entities); return Task.CompletedTask; }
    public Task RemoveAsync<T>(T entity, CancellationToken cancellationToken = default) where T : class { _db.Set<T>().Remove(entity); return Task.CompletedTask; }
}
