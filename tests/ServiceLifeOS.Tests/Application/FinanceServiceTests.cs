using ServiceLifeOS.Application.Ports;
using ServiceLifeOS.Application.Services;
using ServiceLifeOS.Domain.Entities;
using ServiceLifeOS.Dtos.Finances;
using Xunit;

namespace ServiceLifeOS.Tests.Application;

public sealed class FinanceServiceTests
{
    [Fact]
    public async Task CreateTransaction_ConfirmedTransactionCreatesXpGrant()
    {
        var repository = new FakeFinanceRepository();
        repository.Categories.Add(new() { Id = Guid.NewGuid(), UserId = "user-1", Type = FinancialCategoryType.Expense });
        repository.XpRules.Add(new() { UserId = "user-1", EventType = XpEventType.TransactionConfirmed, Amount = 1 });
        var service = CreateService(repository);

        var transaction = await service.CreateTransactionAsync("user-1", new()
        {
            CategoryId = repository.Categories[0].Id,
            Amount = 10,
            TransactionDate = new DateOnly(2026, 8, 12),
            Type = FinancialCategoryType.Expense,
            PaymentMethod = PaymentMethod.Pix,
            Status = TransactionStatus.Confirmed
        });

        var entry = Assert.Single(repository.XpEntries);
        Assert.Equal(transaction.Id, entry.SourceId);
        Assert.Equal(XpLedgerEntryType.Grant, entry.Type);
        Assert.Equal(1, entry.Amount);
    }

    [Fact]
    public async Task DeleteTransaction_ConfirmedTransactionCreatesXpReversal()
    {
        var categoryId = Guid.NewGuid();
        var transactionId = Guid.NewGuid();
        var repository = new FakeFinanceRepository();
        repository.Categories.Add(new() { Id = categoryId, UserId = "user-1", Type = FinancialCategoryType.Expense });
        repository.Transactions.Add(new() { Id = transactionId, UserId = "user-1", CategoryId = categoryId, Amount = 10, TransactionDate = new DateOnly(2026, 8, 12), Type = FinancialCategoryType.Expense, PaymentMethod = PaymentMethod.Pix, Status = TransactionStatus.Confirmed });
        repository.XpEntries.Add(new() { Id = Guid.NewGuid(), UserId = "user-1", Type = XpLedgerEntryType.Grant, Amount = 1, SourceType = "FinancialTransaction", SourceId = transactionId });
        var service = CreateService(repository);

        await service.DeleteTransactionAsync("user-1", transactionId);

        Assert.NotNull(repository.Transactions[0].DeletedAt);
        var reversal = Assert.Single(repository.XpEntries, x => x.Type == XpLedgerEntryType.Reversal);
        Assert.Equal(-1, reversal.Amount);
    }

    [Fact]
    public async Task CreateInstallmentPurchase_AssignsRoundingRemainderToFirstInstallment()
    {
        var repository = new FakeFinanceRepository();
        var categoryId = Guid.NewGuid();
        repository.Categories.Add(new() { Id = categoryId, UserId = "user-1", Type = FinancialCategoryType.Expense });
        var service = CreateService(repository);

        var purchase = await service.CreateInstallmentPurchaseAsync("user-1", new()
        {
            CategoryId = categoryId,
            TotalAmount = 100,
            InstallmentCount = 3,
            FirstInstallmentDate = new DateOnly(2026, 8, 12),
            Status = TransactionStatus.Planned
        });

        Assert.Equal(new decimal[] { 33.34m, 33.33m, 33.33m }, purchase.Installments.Select(x => x.Amount));
        Assert.All(purchase.Installments, x => Assert.Equal(PaymentMethod.InstallmentCredit, x.PaymentMethod));
    }

    [Fact]
    public async Task UpdateInstallmentPurchase_DoesNotReplaceConfirmedInstallments()
    {
        var categoryId = Guid.NewGuid();
        var purchaseId = Guid.NewGuid();
        var repository = new FakeFinanceRepository();
        repository.Categories.Add(new() { Id = categoryId, UserId = "user-1", Type = FinancialCategoryType.Expense });
        repository.Purchases.Add(new() { Id = purchaseId, UserId = "user-1", CategoryId = categoryId, TotalAmount = 100, InstallmentCount = 3 });
        repository.Transactions.AddRange([
            new FinancialTransaction { Id = Guid.NewGuid(), UserId = "user-1", CategoryId = categoryId, InstallmentPurchaseId = purchaseId, InstallmentNumber = 1, Amount = 33.34m, TransactionDate = new DateOnly(2026, 8, 1), Type = FinancialCategoryType.Expense, PaymentMethod = PaymentMethod.InstallmentCredit, Status = TransactionStatus.Confirmed },
            new FinancialTransaction { Id = Guid.NewGuid(), UserId = "user-1", CategoryId = categoryId, InstallmentPurchaseId = purchaseId, InstallmentNumber = 2, Amount = 33.33m, TransactionDate = new DateOnly(2026, 9, 1), Type = FinancialCategoryType.Expense, PaymentMethod = PaymentMethod.InstallmentCredit, Status = TransactionStatus.Planned },
            new FinancialTransaction { Id = Guid.NewGuid(), UserId = "user-1", CategoryId = categoryId, InstallmentPurchaseId = purchaseId, InstallmentNumber = 3, Amount = 33.33m, TransactionDate = new DateOnly(2026, 10, 1), Type = FinancialCategoryType.Expense, PaymentMethod = PaymentMethod.InstallmentCredit, Status = TransactionStatus.Planned }
        ]);
        var service = CreateService(repository);

        var purchase = await service.UpdateInstallmentPurchaseAsync("user-1", purchaseId, new()
        {
            CategoryId = categoryId,
            TotalAmount = 200,
            InstallmentCount = 4,
            FirstInstallmentDate = new DateOnly(2026, 9, 1),
            Status = TransactionStatus.Planned
        });

        Assert.Equal(33.34m, Assert.Single(purchase.Installments, x => x.InstallmentNumber == 1).Amount);
        Assert.Equal(4, purchase.Installments.Count);
        Assert.Equal(2, repository.Transactions.Count(x => x.DeletedAt.HasValue));
    }

    [Fact]
    public async Task GetCategorySpending_ReturnsAtLimitAlertAtOneHundredPercent()
    {
        var categoryId = Guid.NewGuid();
        var budgetId = Guid.NewGuid();
        var repository = new FakeFinanceRepository();
        repository.Categories.Add(new() { Id = categoryId, UserId = "user-1", Name = "Housing", Type = FinancialCategoryType.Expense });
        repository.Budgets.Add(new() { Id = budgetId, CategoryId = categoryId, Amount = 100 });
        repository.Transactions.Add(new() { UserId = "user-1", CategoryId = categoryId, Amount = 100, TransactionDate = new DateOnly(2026, 8, 15), Type = FinancialCategoryType.Expense, Status = TransactionStatus.Confirmed });
        var service = CreateService(repository);

        var result = await service.GetCategorySpendingAsync("user-1", new DateOnly(2026, 8, 1));

        var item = Assert.Single(result);
        Assert.Equal(BudgetAlert.AtLimit, item.Alert);
        Assert.Equal(100m, item.Percentage);
    }

    [Fact]
    public async Task DeleteBudgetOverride_RemovesTheMonthlyException()
    {
        var categoryId = Guid.NewGuid();
        var budgetId = Guid.NewGuid();
        var repository = new FakeFinanceRepository();
        repository.Categories.Add(new() { Id = categoryId, UserId = "user-1", Type = FinancialCategoryType.Expense });
        repository.Budgets.Add(new() { Id = budgetId, CategoryId = categoryId, Amount = 100 });
        repository.Overrides.Add(new() { Id = Guid.NewGuid(), CategoryBudgetId = budgetId, Month = new DateOnly(2026, 8, 1), Amount = 150 });
        var service = CreateService(repository);

        await service.DeleteBudgetOverrideAsync("user-1", categoryId, new DateOnly(2026, 8, 22));

        Assert.Empty(repository.Overrides);
    }

    [Fact]
    public async Task CreateTransaction_UnlocksTransactionBadgeWhenCriterionIsMet()
    {
        var categoryId = Guid.NewGuid();
        var badgeId = Guid.NewGuid();
        var repository = new FakeFinanceRepository();
        repository.Categories.Add(new() { Id = categoryId, UserId = "user-1", Type = FinancialCategoryType.Expense });
        repository.Badges.Add(new() { Id = badgeId, UserId = "user-1" });
        repository.BadgeCriteria.Add(new() { BadgeId = badgeId, Type = BadgeCriterionType.TransactionConfirmationCount, TargetValue = 1 });
        var service = CreateService(repository);

        await service.CreateTransactionAsync("user-1", new() { CategoryId = categoryId, Amount = 10, TransactionDate = new DateOnly(2026, 8, 12), Type = FinancialCategoryType.Expense, PaymentMethod = PaymentMethod.Pix, Status = TransactionStatus.Confirmed });

        Assert.Equal(badgeId, Assert.Single(repository.UserBadges).BadgeId);
    }

    [Fact]
    public async Task GetMonthlyComparison_ContainsProjectedTransactionsForEachMonth()
    {
        var repository = new FakeFinanceRepository();
        repository.Transactions.Add(new() { UserId = "user-1", Amount = 100, TransactionDate = new DateOnly(2026, 8, 10), Type = FinancialCategoryType.Income, Status = TransactionStatus.Planned });
        var service = CreateService(repository);

        var comparison = await service.GetCashFlowProjectionAsync("user-1", new DateOnly(2026, 8, 1), new DateOnly(2026, 9, 1));

        Assert.Equal(2, comparison.Items.Count);
        Assert.Equal(100m, comparison.Items.First().ProjectedIncome);
    }

    private static FinanceService CreateService(FakeFinanceRepository repository)
    {
        return new FinanceService(repository, new FakeAuditLogRepository(), new FakeUnitOfWork());
    }

    private sealed class FakeFinanceRepository : IFinanceRepository
    {
        public List<FinancialCategory> Categories { get; } = [];
        public List<CategoryBudget> Budgets { get; } = [];
        public List<CategoryBudgetOverride> Overrides { get; } = [];
        public List<FinancialTransaction> Transactions { get; } = [];
        public List<RecurringTransaction> Recurrences { get; } = [];
        public List<InstallmentPurchase> Purchases { get; } = [];
        public List<XpEventRule> XpRules { get; } = [];
        public List<XpLedgerEntry> XpEntries { get; } = [];
        public List<Badge> Badges { get; } = [];
        public List<BadgeCriterion> BadgeCriteria { get; } = [];
        public List<UserBadge> UserBadges { get; } = [];
        public Task<IReadOnlyCollection<FinancialCategory>> GetCategoriesAsync(string userId, bool includeArchived, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<FinancialCategory>>(Categories.Where(x => x.UserId == userId && (includeArchived || !x.Archived)).ToArray());
        public Task<FinancialCategory?> GetCategoryAsync(string userId, Guid categoryId, CancellationToken cancellationToken = default) => Task.FromResult(Categories.FirstOrDefault(x => x.UserId == userId && x.Id == categoryId));
        public Task<FinancialTransaction?> GetTransactionAsync(string userId, Guid transactionId, CancellationToken cancellationToken = default) => Task.FromResult(Transactions.FirstOrDefault(x => x.UserId == userId && x.Id == transactionId && x.DeletedAt is null));
        public Task<RecurringTransaction?> GetRecurringTransactionAsync(string userId, Guid recurringTransactionId, CancellationToken cancellationToken = default) => Task.FromResult(Recurrences.FirstOrDefault(x => x.UserId == userId && x.Id == recurringTransactionId));
        public Task<InstallmentPurchase?> GetInstallmentPurchaseAsync(string userId, Guid installmentPurchaseId, CancellationToken cancellationToken = default) => Task.FromResult(Purchases.FirstOrDefault(x => x.UserId == userId && x.Id == installmentPurchaseId));
        public Task<CategoryBudget?> GetBudgetAsync(Guid categoryId, CancellationToken cancellationToken = default) => Task.FromResult(Budgets.FirstOrDefault(x => x.CategoryId == categoryId));
        public Task<CategoryBudgetOverride?> GetBudgetOverrideAsync(Guid budgetId, DateOnly month, CancellationToken cancellationToken = default) => Task.FromResult(Overrides.FirstOrDefault(x => x.CategoryBudgetId == budgetId && x.Month == month));
        public Task<IReadOnlyCollection<FinancialTransaction>> GetTransactionsAsync(string userId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<FinancialTransaction>>(Transactions.Where(x => x.UserId == userId).ToArray());
        public Task<IReadOnlyCollection<FinancialTransaction>> GetTransactionsForInstallmentPurchaseAsync(string userId, Guid installmentPurchaseId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<FinancialTransaction>>(Transactions.Where(x => x.UserId == userId && x.InstallmentPurchaseId == installmentPurchaseId).ToArray());
        public Task<IReadOnlyCollection<RecurringTransaction>> GetRecurringTransactionsAsync(string userId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<RecurringTransaction>>(Recurrences.Where(x => x.UserId == userId).ToArray());
        public Task<IReadOnlyCollection<CategoryBudget>> GetBudgetsAsync(IReadOnlyCollection<Guid> categoryIds, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<CategoryBudget>>(Budgets.Where(x => categoryIds.Contains(x.CategoryId)).ToArray());
        public Task<IReadOnlyCollection<CategoryBudgetOverride>> GetBudgetOverridesAsync(IReadOnlyCollection<Guid> budgetIds, DateOnly month, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<CategoryBudgetOverride>>(Overrides.Where(x => budgetIds.Contains(x.CategoryBudgetId) && x.Month == month).ToArray());
        public Task<XpEventRule?> GetXpRuleAsync(string userId, XpEventType eventType, CancellationToken cancellationToken = default) => Task.FromResult(XpRules.FirstOrDefault(x => x.UserId == userId && x.EventType == eventType));
        public Task<IReadOnlyCollection<XpLedgerEntry>> GetXpEntriesForSourceAsync(string userId, string sourceType, Guid sourceId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<XpLedgerEntry>>(XpEntries.Where(x => x.UserId == userId && x.SourceType == sourceType && x.SourceId == sourceId).ToArray());
        public Task<IReadOnlyCollection<Badge>> GetBadgesAsync(string userId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<Badge>>(Badges.Where(x => x.UserId == userId).ToArray());
        public Task<IReadOnlyCollection<BadgeCriterion>> GetBadgeCriteriaAsync(IReadOnlyCollection<Guid> badgeIds, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<BadgeCriterion>>(BadgeCriteria.Where(x => badgeIds.Contains(x.BadgeId)).ToArray());
        public Task<IReadOnlyCollection<UserBadge>> GetUserBadgesAsync(string userId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<UserBadge>>(UserBadges.Where(x => x.UserId == userId).ToArray());
        public Task AddAsync<T>(T entity, CancellationToken cancellationToken = default) where T : class { Add(entity); return Task.CompletedTask; }
        public Task AddRangeAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default) where T : class { foreach (var entity in entities) Add(entity); return Task.CompletedTask; }
        private void Add<T>(T entity) where T : class
        {
            switch (entity)
            {
                case FinancialCategory value: Categories.Add(value); break;
                case CategoryBudget value: Budgets.Add(value); break;
                case CategoryBudgetOverride value: Overrides.Add(value); break;
                case FinancialTransaction value: Transactions.Add(value); break;
                case RecurringTransaction value: Recurrences.Add(value); break;
                case InstallmentPurchase value: Purchases.Add(value); break;
                case XpLedgerEntry value: XpEntries.Add(value); break;
                case UserBadge value: UserBadges.Add(value); break;
                default: throw new InvalidOperationException("Unsupported entity.");
            }
        }
        public Task RemoveAsync<T>(T entity, CancellationToken cancellationToken = default) where T : class
        {
            if (entity is CategoryBudgetOverride overrideValue) Overrides.Remove(overrideValue);
            if (entity is UserBadge userBadge) UserBadges.Remove(userBadge);
            return Task.CompletedTask;
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
