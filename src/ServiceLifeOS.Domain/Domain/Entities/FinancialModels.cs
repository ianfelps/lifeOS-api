namespace ServiceLifeOS.Domain.Entities;

public sealed class FinancialCategory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public FinancialCategoryType Type { get; set; }
    public bool Archived { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class CategoryBudget
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CategoryId { get; set; }
    public decimal Amount { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class CategoryBudgetOverride
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CategoryBudgetId { get; set; }
    public DateOnly Month { get; set; }
    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class RecurringTransaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public FinancialCategoryType Type { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public DateOnly FirstOccurrenceDate { get; set; }
    public DateTime? EndedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class InstallmentPurchase
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public decimal TotalAmount { get; set; }
    public int InstallmentCount { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class FinancialTransaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public Guid? RecurringTransactionId { get; set; }
    public Guid? InstallmentPurchaseId { get; set; }
    public decimal Amount { get; set; }
    public DateOnly TransactionDate { get; set; }
    public FinancialCategoryType Type { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public TransactionStatus Status { get; set; }
    public int? InstallmentNumber { get; set; }
    public string? Description { get; set; }
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
