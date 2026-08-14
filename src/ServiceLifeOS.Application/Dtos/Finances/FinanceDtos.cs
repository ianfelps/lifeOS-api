using ServiceLifeOS.Domain.Entities;

namespace ServiceLifeOS.Dtos.Finances;

public sealed class FinancialCategoryRequestDto
{
    public string Name { get; set; } = string.Empty;

    public FinancialCategoryType Type { get; set; }
}

public sealed class FinancialCategoryResponseDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public FinancialCategoryType Type { get; set; }

    public bool Archived { get; set; }
}

public sealed class BudgetRequestDto
{
    public decimal Amount { get; set; }
}

public sealed class BudgetOverrideRequestDto
{
    public decimal Amount { get; set; }
}

public sealed class BudgetResponseDto
{
    public Guid CategoryId { get; set; }
    public decimal Amount { get; set; }
    public DateOnly? OverrideMonth { get; set; }
}

public sealed class TransactionRequestDto
{
    public Guid CategoryId { get; set; }
    public decimal Amount { get; set; }
    public DateOnly TransactionDate { get; set; }
    public FinancialCategoryType Type { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public TransactionStatus Status { get; set; }
    public string? Description { get; set; }
}

public sealed class TransactionResponseDto
{
    public Guid Id { get; set; }
    public Guid CategoryId { get; set; }
    public decimal Amount { get; set; }
    public DateOnly TransactionDate { get; set; }
    public FinancialCategoryType Type { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public TransactionStatus Status { get; set; }
    public int? InstallmentNumber { get; set; }
    public Guid? InstallmentPurchaseId { get; set; }
    public Guid? RecurringTransactionId { get; set; }
    public string? Description { get; set; }
}

public sealed class TransactionQueryDto
{
    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 50;

    public DateOnly? From { get; set; }

    public DateOnly? To { get; set; }

    public Guid? CategoryId { get; set; }

    public FinancialCategoryType? Type { get; set; }

    public TransactionStatus? Status { get; set; }

    public PaymentMethod? PaymentMethod { get; set; }

    public string Sort { get; set; } = "date-desc";
}

public sealed class PagedTransactionResponseDto
{
    public IReadOnlyCollection<TransactionResponseDto> Items { get; set; } = [];

    public int Page { get; set; }

    public int PageSize { get; set; }

    public int TotalCount { get; set; }
}

public sealed class RecurringTransactionRequestDto
{
    public Guid CategoryId { get; set; }
    public decimal Amount { get; set; }
    public FinancialCategoryType Type { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public DateOnly FirstOccurrenceDate { get; set; }
    public string? Description { get; set; }
}

public sealed class RecurringTransactionResponseDto
{
    public Guid Id { get; set; }
    public Guid CategoryId { get; set; }
    public decimal Amount { get; set; }
    public FinancialCategoryType Type { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public DateOnly FirstOccurrenceDate { get; set; }
    public DateTime? EndedAt { get; set; }
    public string? Description { get; set; }
}

public sealed class InstallmentPurchaseRequestDto
{
    public Guid CategoryId { get; set; }
    public decimal TotalAmount { get; set; }
    public int InstallmentCount { get; set; }
    public DateOnly FirstInstallmentDate { get; set; }
    public TransactionStatus Status { get; set; }
    public string? Description { get; set; }
}

public sealed class InstallmentPurchaseResponseDto
{
    public Guid Id { get; set; }
    public Guid CategoryId { get; set; }
    public decimal TotalAmount { get; set; }
    public int InstallmentCount { get; set; }
    public string? Description { get; set; }
    public IReadOnlyCollection<TransactionResponseDto> Installments { get; set; } = [];
}

public enum BudgetAlert
{
    None,
    EightyPercent,
    AtLimit,
    Exceeded
}

public sealed class CategorySpendingResponseDto
{
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal Spent { get; set; }
    public decimal? Budget { get; set; }
    public decimal? Remaining { get; set; }
    public decimal? Percentage { get; set; }
    public BudgetAlert Alert { get; set; }
}

public sealed class MonthlySummaryResponseDto
{
    public DateOnly Month { get; set; }
    public decimal ConfirmedIncome { get; set; }
    public decimal ConfirmedExpense { get; set; }
    public decimal ConfirmedBalance { get; set; }
    public decimal ProjectedIncome { get; set; }
    public decimal ProjectedExpense { get; set; }
    public decimal ProjectedBalance { get; set; }
}

public sealed class MonthlyComparisonResponseDto
{
    public IReadOnlyCollection<MonthlySummaryResponseDto> Items { get; set; } = [];
}
