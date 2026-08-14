using Microsoft.AspNetCore.Mvc;
using ServiceLifeOS.Api.Controllers;
using ServiceLifeOS.Application.Ports;
using ServiceLifeOS.Application.Services;
using ServiceLifeOS.Domain.Entities;
using ServiceLifeOS.Dtos.Finances;
using Xunit;

namespace ServiceLifeOS.Tests.Api;

public sealed class FinancesControllerTests
{
    [Fact]
    public async Task CreateTransaction_ReturnsCreatedTransactionForAuthenticatedUser()
    {
        var categoryId = Guid.NewGuid();
        var repository = new FakeFinanceRepository();
        repository.Categories.Add(new()
        {
            Id = categoryId,
            UserId = "user-1",
            Type = FinancialCategoryType.Expense
        });
        var controller = new FinancesController(
            new FinanceService(repository, new FakeAuditLogRepository(), new FakeUnitOfWork()),
            new FakeCurrentUser());

        var result = await controller.CreateTransaction(new()
        {
            CategoryId = categoryId,
            Amount = 10,
            TransactionDate = new DateOnly(2026, 8, 12),
            Type = FinancialCategoryType.Expense,
            PaymentMethod = PaymentMethod.Pix,
            Status = TransactionStatus.Planned
        },
        CancellationToken.None);

        var response = Assert.IsType<CreatedResult>(result.Result);
        Assert.Equal("user-1", Assert.Single(repository.Transactions).UserId);
        Assert.Equal(10m, Assert.IsType<TransactionResponseDto>(response.Value).Amount);
    }

    private sealed class FakeCurrentUser : ICurrentUser
    {
        public string UserId => "user-1";
        public string UserName => "user";
        public string TokenId => "token";
    }

    private sealed class FakeFinanceRepository : IFinanceRepository
    {
        public List<FinancialCategory> Categories { get; } = [];
        public List<FinancialTransaction> Transactions { get; } = [];
        public Task<IReadOnlyCollection<FinancialCategory>> GetCategoriesAsync(
            string userId,
            bool includeArchived,
            CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<FinancialCategory>>([]);

        public Task<FinancialCategory?> GetCategoryAsync(
            string userId,
            Guid categoryId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Categories.FirstOrDefault(x => x.UserId == userId && x.Id == categoryId));

        public Task<FinancialTransaction?> GetTransactionAsync(
            string userId,
            Guid transactionId,
            CancellationToken cancellationToken = default) => Task.FromResult<FinancialTransaction?>(null);

        public Task<RecurringTransaction?> GetRecurringTransactionAsync(
            string userId,
            Guid recurringTransactionId,
            CancellationToken cancellationToken = default) => Task.FromResult<RecurringTransaction?>(null);

        public Task<InstallmentPurchase?> GetInstallmentPurchaseAsync(
            string userId,
            Guid installmentPurchaseId,
            CancellationToken cancellationToken = default) => Task.FromResult<InstallmentPurchase?>(null);

        public Task<CategoryBudget?> GetBudgetAsync(
            Guid categoryId,
            CancellationToken cancellationToken = default) => Task.FromResult<CategoryBudget?>(null);

        public Task<CategoryBudgetOverride?> GetBudgetOverrideAsync(
            Guid budgetId,
            DateOnly month,
            CancellationToken cancellationToken = default) => Task.FromResult<CategoryBudgetOverride?>(null);

        public Task<IReadOnlyCollection<FinancialTransaction>> GetTransactionsAsync(
            string userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<FinancialTransaction>>(
                Transactions.Where(x => x.UserId == userId).ToArray());

        public Task<IReadOnlyCollection<FinancialTransaction>> GetTransactionsForInstallmentPurchaseAsync(
            string userId,
            Guid installmentPurchaseId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<FinancialTransaction>>([]);

        public Task<IReadOnlyCollection<RecurringTransaction>> GetRecurringTransactionsAsync(
            string userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<RecurringTransaction>>([]);

        public Task<IReadOnlyCollection<CategoryBudget>> GetBudgetsAsync(
            IReadOnlyCollection<Guid> categoryIds,
            CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<CategoryBudget>>([]);

        public Task<IReadOnlyCollection<CategoryBudgetOverride>> GetBudgetOverridesAsync(
            IReadOnlyCollection<Guid> budgetIds,
            DateOnly month,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<CategoryBudgetOverride>>([]);

        public Task<XpEventRule?> GetXpRuleAsync(
            string userId,
            XpEventType eventType,
            CancellationToken cancellationToken = default) => Task.FromResult<XpEventRule?>(null);

        public Task<IReadOnlyCollection<XpLedgerEntry>> GetXpEntriesForSourceAsync(
            string userId,
            string sourceType,
            Guid sourceId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<XpLedgerEntry>>([]);

        public Task<IReadOnlyCollection<Badge>> GetBadgesAsync(
            string userId,
            CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<Badge>>([]);

        public Task<IReadOnlyCollection<BadgeCriterion>> GetBadgeCriteriaAsync(
            IReadOnlyCollection<Guid> badgeIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<BadgeCriterion>>([]);

        public Task<IReadOnlyCollection<UserBadge>> GetUserBadgesAsync(
            string userId,
            CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<UserBadge>>([]);

        public Task AddAsync<T>(T entity, CancellationToken cancellationToken = default)
            where T : class
        {
            if (entity is FinancialTransaction transaction)
            {
                Transactions.Add(transaction);
            }

            return Task.CompletedTask;
        }

        public Task AddRangeAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default)
            where T : class => Task.CompletedTask;

        public Task RemoveAsync<T>(T entity, CancellationToken cancellationToken = default)
            where T : class => Task.CompletedTask;
    }

    private sealed class FakeAuditLogRepository : IAuditLogRepository
    {
        public Task CreateAsync(AuditLog auditLog, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<AuditLogPage> GetPageAsync(
            string userId,
            AuditLogFilter filter,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new AuditLogPage());
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(1);
        }

        public Task<IAppTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}
