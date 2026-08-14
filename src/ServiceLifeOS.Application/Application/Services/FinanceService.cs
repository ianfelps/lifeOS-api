using System.Text.Json;
using ServiceLifeOS.Application.Ports;
using ServiceLifeOS.Domain.Entities;
using ServiceLifeOS.Dtos.Finances;

namespace ServiceLifeOS.Application.Services;

public sealed class FinanceService
{
    private readonly IFinanceRepository _finances;
    private readonly IAuditLogRepository _auditLogs;
    private readonly IUnitOfWork _unitOfWork;

    public FinanceService(
        IFinanceRepository finances,
        IAuditLogRepository auditLogs,
        IUnitOfWork unitOfWork)
    {
        _finances = finances;
        _auditLogs = auditLogs;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyCollection<FinancialCategoryResponseDto>> GetCategoriesAsync(
        string userId,
        bool includeArchived,
        CancellationToken cancellationToken = default)
    {
        return (await _finances.GetCategoriesAsync(userId, includeArchived, cancellationToken))
            .Select(MapCategory)
            .ToArray();
    }

    public async Task<FinancialCategoryResponseDto> CreateCategoryAsync(
        string userId,
        FinancialCategoryRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ValidateCategory(request);
        var now = DateTime.UtcNow;
        var category = new FinancialCategory
        {
            UserId = userId,
            Name = request.Name.Trim(),
            Type = request.Type,
            CreatedAt = now,
            UpdatedAt = now
        };
        await _finances.AddAsync(category, cancellationToken);
        await AuditAsync(
            userId,
            AuditAction.Created,
            "FinancialCategory",
            category.Id,
            null,
            category,
            now,
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapCategory(category);
    }

    public async Task<FinancialCategoryResponseDto> UpdateCategoryAsync(
        string userId,
        Guid categoryId,
        FinancialCategoryRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ValidateCategory(request);
        var category = await RequiredCategoryAsync(userId, categoryId, cancellationToken);
        var previous = new { category.Name, category.Type };
        category.Name = request.Name.Trim();
        category.Type = request.Type;
        category.UpdatedAt = DateTime.UtcNow;
        await AuditAsync(
            userId,
            AuditAction.Updated,
            "FinancialCategory",
            category.Id,
            previous,
            category,
            category.UpdatedAt,
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapCategory(category);
    }

    public async Task ArchiveCategoryAsync(
        string userId,
        Guid categoryId,
        CancellationToken cancellationToken = default)
    {
        var category = await RequiredCategoryAsync(userId, categoryId, cancellationToken);
        if (category.Archived)
        {
            return;
        }
        category.Archived = true;
        category.UpdatedAt = DateTime.UtcNow;
        await AuditAsync(
            userId,
            AuditAction.Archived,
            "FinancialCategory",
            category.Id,
            null,
            category,
            category.UpdatedAt,
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<BudgetResponseDto?> GetBudgetAsync(
        string userId,
        Guid categoryId,
        DateOnly? month,
        CancellationToken cancellationToken = default)
    {
        await RequiredCategoryAsync(userId, categoryId, cancellationToken);
        var budget = await _finances.GetBudgetAsync(categoryId, cancellationToken);
        if (budget is null)
        {
            return null;
        }
        var normalizedMonth = month.HasValue ? FirstDayOfMonth(month.Value) : (DateOnly?)null;
        var overrideValue = normalizedMonth.HasValue
            ? await _finances.GetBudgetOverrideAsync(budget.Id, normalizedMonth.Value, cancellationToken)
            : null;
        return new()
        {
            CategoryId = categoryId,
            Amount = overrideValue?.Amount ?? budget.Amount,
            OverrideMonth = overrideValue is null ? null : normalizedMonth
        };
    }

    public async Task<BudgetResponseDto> SetBudgetAsync(
        string userId,
        Guid categoryId,
        BudgetRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ValidateAmount(request.Amount);
        var category = await RequiredCategoryAsync(userId, categoryId, cancellationToken);
        if (category.Type != FinancialCategoryType.Expense)
        {
            throw new ArgumentException("Budgets are only available for expense categories.");
        }
        var now = DateTime.UtcNow;
        var budget = await _finances.GetBudgetAsync(categoryId, cancellationToken);
        if (budget is null)
        {
            budget = new()
            {
                CategoryId = categoryId,
                Amount = request.Amount,
                CreatedAt = now,
                UpdatedAt = now
            };
            await _finances.AddAsync(budget, cancellationToken);
            await AuditAsync(userId, AuditAction.Created, "CategoryBudget", budget.Id, null, budget, now, cancellationToken);
        }
        else
        {
            var previous = budget.Amount;
            budget.Amount = request.Amount;
            budget.UpdatedAt = now;
            await AuditAsync(
                userId,
                AuditAction.Updated,
                "CategoryBudget",
                budget.Id,
                previous,
                budget.Amount,
                now,
                cancellationToken);
        }
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return new()
        {
            CategoryId = categoryId,
            Amount = budget.Amount
        };
    }

    public async Task<BudgetResponseDto> SetBudgetOverrideAsync(string userId, Guid categoryId, DateOnly month, BudgetOverrideRequestDto request, CancellationToken cancellationToken = default)
    {
        ValidateAmount(request.Amount);
        var budget = await GetRequiredBudgetAsync(userId, categoryId, cancellationToken);
        var normalizedMonth = FirstDayOfMonth(month);
        var now = DateTime.UtcNow;
        var overrideValue = await _finances.GetBudgetOverrideAsync(budget.Id, normalizedMonth, cancellationToken);
        if (overrideValue is null)
        {
            overrideValue = new() { CategoryBudgetId = budget.Id, Month = normalizedMonth, Amount = request.Amount, CreatedAt = now, UpdatedAt = now };
            await _finances.AddAsync(overrideValue, cancellationToken);
            await AuditAsync(userId, AuditAction.Created, "CategoryBudgetOverride", overrideValue.Id, null, overrideValue, now, cancellationToken);
        }
        else
        {
            overrideValue.Amount = request.Amount;
            overrideValue.UpdatedAt = now;
            await AuditAsync(userId, AuditAction.Updated, "CategoryBudgetOverride", overrideValue.Id, null, overrideValue, now, cancellationToken);
        }
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return new() { CategoryId = categoryId, Amount = overrideValue.Amount, OverrideMonth = normalizedMonth };
    }

    public async Task DeleteBudgetOverrideAsync(string userId, Guid categoryId, DateOnly month, CancellationToken cancellationToken = default)
    {
        var budget = await GetRequiredBudgetAsync(userId, categoryId, cancellationToken);
        var normalizedMonth = FirstDayOfMonth(month);
        var overrideValue = await _finances.GetBudgetOverrideAsync(budget.Id, normalizedMonth, cancellationToken)
            ?? throw new KeyNotFoundException("Category budget override was not found.");
        await _finances.RemoveAsync(overrideValue, cancellationToken);
        await AuditAsync(userId, AuditAction.Deleted, "CategoryBudgetOverride", overrideValue.Id, overrideValue, null, DateTime.UtcNow, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<TransactionResponseDto> CreateTransactionAsync(string userId, TransactionRequestDto request, CancellationToken cancellationToken = default)
    {
        ValidateTransaction(request);
        await ValidateCategoryForTransactionAsync(userId, request.CategoryId, request.Type, cancellationToken);
        var now = DateTime.UtcNow;
        var transaction = new FinancialTransaction { UserId = userId, CategoryId = request.CategoryId, Amount = request.Amount, TransactionDate = request.TransactionDate, Type = request.Type, PaymentMethod = request.PaymentMethod, Status = request.Status, Description = TrimOrNull(request.Description), CreatedAt = now, UpdatedAt = now };
        await _finances.AddAsync(transaction, cancellationToken);
        await SyncTransactionXpAsync(transaction, now, cancellationToken);
        await AuditAsync(userId, AuditAction.Created, "FinancialTransaction", transaction.Id, null, transaction, now, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapTransaction(transaction);
    }

    public async Task<TransactionResponseDto> UpdateTransactionAsync(string userId, Guid transactionId, TransactionRequestDto request, CancellationToken cancellationToken = default)
    {
        ValidateTransaction(request);
        var transaction = await RequiredTransactionAsync(userId, transactionId, cancellationToken);
        if (transaction.InstallmentPurchaseId.HasValue)
        {
            throw new InvalidOperationException("Installments must be edited through their purchase.");
        }
        await ValidateCategoryForTransactionAsync(userId, request.CategoryId, request.Type, cancellationToken);
        var previous = new { transaction.CategoryId, transaction.Amount, transaction.TransactionDate, transaction.Type, transaction.PaymentMethod, transaction.Status, transaction.Description };
        transaction.CategoryId = request.CategoryId;
        transaction.Amount = request.Amount;
        transaction.TransactionDate = request.TransactionDate;
        transaction.Type = request.Type;
        transaction.PaymentMethod = request.PaymentMethod;
        transaction.Status = request.Status;
        transaction.Description = TrimOrNull(request.Description);
        transaction.UpdatedAt = DateTime.UtcNow;
        await SyncTransactionXpAsync(transaction, transaction.UpdatedAt, cancellationToken);
        await AuditAsync(userId, AuditAction.Updated, "FinancialTransaction", transaction.Id, previous, transaction, transaction.UpdatedAt, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapTransaction(transaction);
    }

    public async Task<TransactionResponseDto> ConfirmTransactionAsync(string userId, Guid transactionId, CancellationToken cancellationToken = default)
    {
        var transaction = await RequiredTransactionAsync(userId, transactionId, cancellationToken);
        transaction.Status = TransactionStatus.Confirmed;
        transaction.UpdatedAt = DateTime.UtcNow;
        await SyncTransactionXpAsync(transaction, transaction.UpdatedAt, cancellationToken);
        await AuditAsync(userId, AuditAction.Updated, "FinancialTransaction", transaction.Id, null, transaction, transaction.UpdatedAt, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapTransaction(transaction);
    }

    public async Task DeleteTransactionAsync(string userId, Guid transactionId, CancellationToken cancellationToken = default)
    {
        var transaction = await RequiredTransactionAsync(userId, transactionId, cancellationToken);
        if (transaction.InstallmentPurchaseId.HasValue)
        {
            throw new InvalidOperationException("Installments must be edited through their purchase.");
        }
        transaction.DeletedAt = DateTime.UtcNow;
        transaction.UpdatedAt = transaction.DeletedAt.Value;
        await SyncTransactionXpAsync(transaction, transaction.UpdatedAt, cancellationToken);
        await AuditAsync(userId, AuditAction.Deleted, "FinancialTransaction", transaction.Id, null, transaction, transaction.UpdatedAt, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<PagedTransactionResponseDto> GetTransactionsAsync(string userId, TransactionQueryDto query, CancellationToken cancellationToken = default)
    {
        ValidateQuery(query);
        await MaterializeRecurringTransactionsAsync(userId, LocalToday(), cancellationToken);
        var transactions = (await _finances.GetTransactionsAsync(userId, cancellationToken)).Where(x => x.DeletedAt is null);
        if (query.From.HasValue) transactions = transactions.Where(x => x.TransactionDate >= query.From.Value);
        if (query.To.HasValue) transactions = transactions.Where(x => x.TransactionDate <= query.To.Value);
        if (query.CategoryId.HasValue) transactions = transactions.Where(x => x.CategoryId == query.CategoryId.Value);
        if (query.Type.HasValue) transactions = transactions.Where(x => x.Type == query.Type.Value);
        if (query.PaymentMethod.HasValue) transactions = transactions.Where(x => x.PaymentMethod == query.PaymentMethod.Value);
        if (query.Status.HasValue) transactions = transactions.Where(x => EffectiveStatus(x) == query.Status.Value);
        transactions = query.Sort switch
        {
            "date-asc" => transactions.OrderBy(x => x.TransactionDate).ThenBy(x => x.Id),
            "amount-asc" => transactions.OrderBy(x => x.Amount).ThenBy(x => x.Id),
            "amount-desc" => transactions.OrderByDescending(x => x.Amount).ThenBy(x => x.Id),
            _ => transactions.OrderByDescending(x => x.TransactionDate).ThenByDescending(x => x.Id)
        };
        var totalCount = transactions.Count();
        return new() { Items = transactions.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).Select(MapTransaction).ToArray(), Page = query.Page, PageSize = query.PageSize, TotalCount = totalCount };
    }

    public async Task<RecurringTransactionResponseDto> CreateRecurringTransactionAsync(string userId, RecurringTransactionRequestDto request, CancellationToken cancellationToken = default)
    {
        ValidateRecurring(request);
        await ValidateCategoryForTransactionAsync(userId, request.CategoryId, request.Type, cancellationToken);
        var now = DateTime.UtcNow;
        var recurrence = new RecurringTransaction { UserId = userId, CategoryId = request.CategoryId, Amount = request.Amount, Type = request.Type, PaymentMethod = request.PaymentMethod, FirstOccurrenceDate = request.FirstOccurrenceDate, Description = TrimOrNull(request.Description), CreatedAt = now, UpdatedAt = now };
        await _finances.AddAsync(recurrence, cancellationToken);
        await MaterializeRecurringTransactionsAsync(userId, LocalToday(), cancellationToken);
        await AuditAsync(userId, AuditAction.Created, "RecurringTransaction", recurrence.Id, null, recurrence, now, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapRecurrence(recurrence);
    }

    public async Task<RecurringTransactionResponseDto> UpdateRecurringTransactionAsync(string userId, Guid recurrenceId, RecurringTransactionRequestDto request, CancellationToken cancellationToken = default)
    {
        ValidateRecurring(request);
        var recurrence = await RequiredRecurringTransactionAsync(userId, recurrenceId, cancellationToken);
        if (recurrence.EndedAt.HasValue)
        {
            throw new InvalidOperationException("Ended recurrences cannot be edited.");
        }
        await ValidateCategoryForTransactionAsync(userId, request.CategoryId, request.Type, cancellationToken);
        var now = DateTime.UtcNow;
        recurrence.EndedAt = now;
        recurrence.UpdatedAt = now;
        var successor = new RecurringTransaction { UserId = userId, CategoryId = request.CategoryId, Amount = request.Amount, Type = request.Type, PaymentMethod = request.PaymentMethod, FirstOccurrenceDate = request.FirstOccurrenceDate > LocalToday() ? request.FirstOccurrenceDate : FirstDayOfNextMonth(LocalToday()), Description = TrimOrNull(request.Description), CreatedAt = now, UpdatedAt = now };
        await _finances.AddAsync(successor, cancellationToken);
        await MaterializeRecurringTransactionsAsync(userId, LocalToday(), cancellationToken);
        await AuditAsync(userId, AuditAction.Updated, "RecurringTransaction", recurrence.Id, recurrence, successor, now, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapRecurrence(successor);
    }

    public async Task EndRecurringTransactionAsync(string userId, Guid recurrenceId, CancellationToken cancellationToken = default)
    {
        var recurrence = await RequiredRecurringTransactionAsync(userId, recurrenceId, cancellationToken);
        if (recurrence.EndedAt.HasValue) return;
        recurrence.EndedAt = DateTime.UtcNow;
        recurrence.UpdatedAt = recurrence.EndedAt.Value;
        await AuditAsync(userId, AuditAction.Updated, "RecurringTransaction", recurrence.Id, null, recurrence, recurrence.UpdatedAt, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<RecurringTransactionResponseDto>> GetRecurringTransactionsAsync(string userId, CancellationToken cancellationToken = default)
    {
        return (await _finances.GetRecurringTransactionsAsync(userId, cancellationToken)).Select(MapRecurrence).ToArray();
    }

    public async Task<InstallmentPurchaseResponseDto> CreateInstallmentPurchaseAsync(string userId, InstallmentPurchaseRequestDto request, CancellationToken cancellationToken = default)
    {
        ValidateInstallmentPurchase(request);
        await ValidateCategoryForTransactionAsync(userId, request.CategoryId, FinancialCategoryType.Expense, cancellationToken);
        var now = DateTime.UtcNow;
        var purchase = new InstallmentPurchase { UserId = userId, CategoryId = request.CategoryId, TotalAmount = request.TotalAmount, InstallmentCount = request.InstallmentCount, Description = TrimOrNull(request.Description), CreatedAt = now, UpdatedAt = now };
        var installments = CreateInstallments(userId, purchase, request.FirstInstallmentDate, request.Status, now);
        await _finances.AddAsync(purchase, cancellationToken);
        await _finances.AddRangeAsync(installments, cancellationToken);
        foreach (var installment in installments) await SyncTransactionXpAsync(installment, now, cancellationToken);
        await AuditAsync(userId, AuditAction.Created, "InstallmentPurchase", purchase.Id, null, purchase, now, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapPurchase(purchase, installments);
    }

    public async Task<InstallmentPurchaseResponseDto> UpdateInstallmentPurchaseAsync(string userId, Guid purchaseId, InstallmentPurchaseRequestDto request, CancellationToken cancellationToken = default)
    {
        ValidateInstallmentPurchase(request);
        await ValidateCategoryForTransactionAsync(userId, request.CategoryId, FinancialCategoryType.Expense, cancellationToken);
        var purchase = await RequiredInstallmentPurchaseAsync(userId, purchaseId, cancellationToken);
        var installments = (await _finances.GetTransactionsForInstallmentPurchaseAsync(userId, purchaseId, cancellationToken)).Where(x => x.DeletedAt is null).OrderBy(x => x.InstallmentNumber).ToArray();
        var confirmed = installments.Where(x => x.Status == TransactionStatus.Confirmed).ToArray();
        var confirmedAmount = confirmed.Sum(x => x.Amount);
        if (request.InstallmentCount < confirmed.Length || request.TotalAmount < confirmedAmount)
        {
            throw new InvalidOperationException("The updated purchase cannot be less than its confirmed installments.");
        }
        var now = DateTime.UtcNow;
        var future = installments.Where(x => x.Status != TransactionStatus.Confirmed).ToArray();
        foreach (var installment in future)
        {
            installment.DeletedAt = now;
            installment.UpdatedAt = now;
            await SyncTransactionXpAsync(installment, now, cancellationToken);
        }
        purchase.CategoryId = request.CategoryId;
        purchase.TotalAmount = request.TotalAmount;
        purchase.InstallmentCount = request.InstallmentCount;
        purchase.Description = TrimOrNull(request.Description);
        purchase.UpdatedAt = now;
        var remainingCount = request.InstallmentCount - confirmed.Length;
        var replacement = CreateInstallments(userId, purchase, request.FirstInstallmentDate, request.Status, now, confirmed.Length + 1, remainingCount, request.TotalAmount - confirmedAmount);
        await _finances.AddRangeAsync(replacement, cancellationToken);
        foreach (var installment in replacement) await SyncTransactionXpAsync(installment, now, cancellationToken);
        await AuditAsync(userId, AuditAction.Updated, "InstallmentPurchase", purchase.Id, null, purchase, now, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapPurchase(purchase, confirmed.Concat(replacement));
    }

    public async Task<InstallmentPurchaseResponseDto> GetInstallmentPurchaseAsync(string userId, Guid purchaseId, CancellationToken cancellationToken = default)
    {
        var purchase = await RequiredInstallmentPurchaseAsync(userId, purchaseId, cancellationToken);
        var installments = (await _finances.GetTransactionsForInstallmentPurchaseAsync(userId, purchaseId, cancellationToken)).Where(x => x.DeletedAt is null);
        return MapPurchase(purchase, installments);
    }

    public async Task<MonthlySummaryResponseDto> GetMonthlySummaryAsync(string userId, DateOnly month, CancellationToken cancellationToken = default)
    {
        var normalizedMonth = FirstDayOfMonth(month);
        await MaterializeRecurringTransactionsAsync(userId, LastDayOfMonth(normalizedMonth), cancellationToken);
        var transactions = (await _finances.GetTransactionsAsync(userId, cancellationToken)).Where(x => x.DeletedAt is null && FirstDayOfMonth(x.TransactionDate) == normalizedMonth).ToArray();
        return BuildMonthlySummary(normalizedMonth, transactions);
    }

    public async Task<MonthlyComparisonResponseDto> GetMonthlyComparisonAsync(string userId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        var start = FirstDayOfMonth(from);
        var end = FirstDayOfMonth(to);
        if (start > end) throw new ArgumentException("The start month must be before the end month.");
        await MaterializeRecurringTransactionsAsync(userId, LastDayOfMonth(end), cancellationToken);
        var transactions = (await _finances.GetTransactionsAsync(userId, cancellationToken)).Where(x => x.DeletedAt is null).ToArray();
        var items = new List<MonthlySummaryResponseDto>();
        for (var month = start; month <= end; month = month.AddMonths(1)) items.Add(BuildMonthlySummary(month, transactions.Where(x => FirstDayOfMonth(x.TransactionDate) == month)));
        return new() { Items = items };
    }

    public Task<MonthlyComparisonResponseDto> GetCashFlowProjectionAsync(string userId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        return GetMonthlyComparisonAsync(userId, from, to, cancellationToken);
    }

    public async Task<IReadOnlyCollection<CategorySpendingResponseDto>> GetCategorySpendingAsync(string userId, DateOnly month, CancellationToken cancellationToken = default)
    {
        var normalizedMonth = FirstDayOfMonth(month);
        await MaterializeRecurringTransactionsAsync(userId, LastDayOfMonth(normalizedMonth), cancellationToken);
        var categories = (await _finances.GetCategoriesAsync(userId, true, cancellationToken)).Where(x => x.Type == FinancialCategoryType.Expense).ToArray();
        var budgets = await _finances.GetBudgetsAsync(categories.Select(x => x.Id).ToArray(), cancellationToken);
        var overrides = await _finances.GetBudgetOverridesAsync(budgets.Select(x => x.Id).ToArray(), normalizedMonth, cancellationToken);
        var transactions = await _finances.GetTransactionsAsync(userId, cancellationToken);
        return categories.Select(category =>
        {
            var budget = budgets.FirstOrDefault(x => x.CategoryId == category.Id);
            decimal? amount = budget is null ? null : overrides.FirstOrDefault(x => x.CategoryBudgetId == budget.Id)?.Amount ?? budget.Amount;
            var spent = transactions.Where(x => x.DeletedAt is null && x.Status == TransactionStatus.Confirmed && x.Type == FinancialCategoryType.Expense && x.CategoryId == category.Id && FirstDayOfMonth(x.TransactionDate) == normalizedMonth).Sum(x => x.Amount);
            decimal? percentage = amount.HasValue ? spent / amount.Value * 100 : null;
            return new CategorySpendingResponseDto { CategoryId = category.Id, CategoryName = category.Name, Spent = spent, Budget = amount, Remaining = amount - spent, Percentage = percentage, Alert = GetBudgetAlert(percentage) };
        }).ToArray();
    }

    private async Task MaterializeRecurringTransactionsAsync(string userId, DateOnly through, CancellationToken cancellationToken)
    {
        var recurrences = await _finances.GetRecurringTransactionsAsync(userId, cancellationToken);
        var existing = await _finances.GetTransactionsAsync(userId, cancellationToken);
        var created = new List<FinancialTransaction>();
        foreach (var recurrence in recurrences.Where(x => !x.EndedAt.HasValue))
        {
            for (var date = recurrence.FirstOccurrenceDate; date <= through; date = date.AddMonths(1))
            {
                if (!existing.Any(x => x.RecurringTransactionId == recurrence.Id && x.TransactionDate == date))
                {
                    created.Add(new() { UserId = userId, CategoryId = recurrence.CategoryId, RecurringTransactionId = recurrence.Id, Amount = recurrence.Amount, TransactionDate = date, Type = recurrence.Type, PaymentMethod = recurrence.PaymentMethod, Status = TransactionStatus.Planned, Description = recurrence.Description, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
                }
            }
        }
        if (created.Count > 0)
        {
            await _finances.AddRangeAsync(created, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task SyncTransactionXpAsync(FinancialTransaction transaction, DateTime now, CancellationToken cancellationToken)
    {
        var entries = await _finances.GetXpEntriesForSourceAsync(transaction.UserId, "FinancialTransaction", transaction.Id, cancellationToken);
        var activeGrant = entries.FirstOrDefault(x => x.Type == XpLedgerEntryType.Grant && !entries.Any(y => y.Type == XpLedgerEntryType.Reversal && y.ReversedEntryId == x.Id));
        var qualifies = transaction.DeletedAt is null && transaction.Status == TransactionStatus.Confirmed;
        if (qualifies && activeGrant is null)
        {
            var rule = await _finances.GetXpRuleAsync(transaction.UserId, XpEventType.TransactionConfirmed, cancellationToken);
            if (rule is not null)
            {
                await _finances.AddAsync(new XpLedgerEntry { UserId = transaction.UserId, Type = XpLedgerEntryType.Grant, Amount = rule.Amount, EventType = XpEventType.TransactionConfirmed, SourceType = "FinancialTransaction", SourceId = transaction.Id, CreatedAt = now }, cancellationToken);
            }
        }
        if (!qualifies && activeGrant is not null)
        {
            await _finances.AddAsync(new XpLedgerEntry { UserId = transaction.UserId, Type = XpLedgerEntryType.Reversal, Amount = -activeGrant.Amount, EventType = XpEventType.TransactionConfirmed, SourceType = "FinancialTransaction", SourceId = transaction.Id, ReversedEntryId = activeGrant.Id, CreatedAt = now }, cancellationToken);
        }
        await RecalculateFinancialBadgesAsync(transaction.UserId, now, cancellationToken);
    }

    private async Task RecalculateFinancialBadgesAsync(string userId, DateTime now, CancellationToken cancellationToken)
    {
        var badges = await _finances.GetBadgesAsync(userId, cancellationToken);
        var criteria = await _finances.GetBadgeCriteriaAsync(badges.Select(x => x.Id).ToArray(), cancellationToken);
        var transactions = await _finances.GetTransactionsAsync(userId, cancellationToken);
        var confirmedCount = transactions.Count(x => x.DeletedAt is null && x.Status == TransactionStatus.Confirmed);
        var unlocked = await _finances.GetUserBadgesAsync(userId, cancellationToken);
        foreach (var badge in badges)
        {
            var badgeCriteria = criteria.Where(x => x.BadgeId == badge.Id).ToArray();
            if (badgeCriteria.Length == 0 || badgeCriteria.Any(x => x.Type != BadgeCriterionType.TransactionConfirmationCount))
            {
                continue;
            }
            var meetsCriteria = badgeCriteria.All(x => confirmedCount >= x.TargetValue);
            var existing = unlocked.FirstOrDefault(x => x.BadgeId == badge.Id);
            if (meetsCriteria && existing is null)
            {
                await _finances.AddAsync(new UserBadge { UserId = userId, BadgeId = badge.Id, UnlockedAt = now }, cancellationToken);
            }
            else if (!meetsCriteria && existing is not null)
            {
                await _finances.RemoveAsync(existing, cancellationToken);
            }
        }
    }

    private async Task<FinancialCategory> RequiredCategoryAsync(string userId, Guid categoryId, CancellationToken cancellationToken) => await _finances.GetCategoryAsync(userId, categoryId, cancellationToken) ?? throw new KeyNotFoundException("Financial category was not found.");
    private async Task<FinancialTransaction> RequiredTransactionAsync(string userId, Guid transactionId, CancellationToken cancellationToken) => await _finances.GetTransactionAsync(userId, transactionId, cancellationToken) ?? throw new KeyNotFoundException("Financial transaction was not found.");
    private async Task<RecurringTransaction> RequiredRecurringTransactionAsync(string userId, Guid id, CancellationToken cancellationToken) => await _finances.GetRecurringTransactionAsync(userId, id, cancellationToken) ?? throw new KeyNotFoundException("Recurring transaction was not found.");
    private async Task<InstallmentPurchase> RequiredInstallmentPurchaseAsync(string userId, Guid id, CancellationToken cancellationToken) => await _finances.GetInstallmentPurchaseAsync(userId, id, cancellationToken) ?? throw new KeyNotFoundException("Installment purchase was not found.");
    private async Task<CategoryBudget> GetRequiredBudgetAsync(string userId, Guid categoryId, CancellationToken cancellationToken)
    {
        await RequiredCategoryAsync(userId, categoryId, cancellationToken);
        return await _finances.GetBudgetAsync(categoryId, cancellationToken) ?? throw new KeyNotFoundException("Category budget was not found.");
    }
    private async Task ValidateCategoryForTransactionAsync(string userId, Guid categoryId, FinancialCategoryType type, CancellationToken cancellationToken)
    {
        var category = await RequiredCategoryAsync(userId, categoryId, cancellationToken);
        if (category.Archived || category.Type != type) throw new ArgumentException("The category is unavailable for this transaction type.");
    }
    private async Task AuditAsync(string userId, AuditAction action, string resourceType, Guid resourceId, object? previous, object? current, DateTime now, CancellationToken cancellationToken)
    {
        await _auditLogs.CreateAsync(new() { UserId = userId, Action = action, ResourceType = resourceType, ResourceId = resourceId, PreviousValues = previous is null ? null : JsonSerializer.Serialize(previous), CurrentValues = current is null ? null : JsonSerializer.Serialize(current), CreatedAt = now }, cancellationToken);
    }
    private static void ValidateCategory(FinancialCategoryRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 120 || !Enum.IsDefined(request.Type)) throw new ArgumentException("Financial category is invalid.");
    }
    private static void ValidateAmount(decimal amount) { if (amount <= 0) throw new ArgumentException("Amount must be greater than zero."); }
    private static void ValidateTransaction(TransactionRequestDto request)
    {
        ValidateAmount(request.Amount);
        if (!Enum.IsDefined(request.Type) || !Enum.IsDefined(request.PaymentMethod) || !Enum.IsDefined(request.Status) || request.Status == TransactionStatus.Overdue || request.PaymentMethod == PaymentMethod.InstallmentCredit) throw new ArgumentException("Financial transaction is invalid.");
    }
    private static void ValidateRecurring(RecurringTransactionRequestDto request)
    {
        ValidateAmount(request.Amount);
        if (!Enum.IsDefined(request.Type) || !Enum.IsDefined(request.PaymentMethod) || request.PaymentMethod == PaymentMethod.InstallmentCredit) throw new ArgumentException("Recurring transaction is invalid.");
    }
    private static void ValidateInstallmentPurchase(InstallmentPurchaseRequestDto request)
    {
        ValidateAmount(request.TotalAmount);
        if (request.InstallmentCount < 2 || !Enum.IsDefined(request.Status) || request.Status == TransactionStatus.Overdue) throw new ArgumentException("Installment purchase is invalid.");
    }
    private static void ValidateQuery(TransactionQueryDto query)
    {
        if (query.Page < 1 || query.PageSize is < 1 or > 100 || (query.From.HasValue && query.To.HasValue && query.From > query.To) || query.Sort is not ("date-desc" or "date-asc" or "amount-desc" or "amount-asc")) throw new ArgumentException("Transaction query is invalid.");
    }
    private static IReadOnlyCollection<FinancialTransaction> CreateInstallments(string userId, InstallmentPurchase purchase, DateOnly firstDate, TransactionStatus status, DateTime now, int firstNumber = 1, int? count = null, decimal? total = null)
    {
        var installmentCount = count ?? purchase.InstallmentCount;
        var installmentTotal = total ?? purchase.TotalAmount;
        var baseAmount = decimal.Floor(installmentTotal / installmentCount * 100) / 100;
        var firstAmount = installmentTotal - baseAmount * (installmentCount - 1);
        return Enumerable.Range(0, installmentCount).Select(index => new FinancialTransaction { UserId = userId, CategoryId = purchase.CategoryId, InstallmentPurchaseId = purchase.Id, Amount = index == 0 ? firstAmount : baseAmount, TransactionDate = firstDate.AddMonths(index), Type = FinancialCategoryType.Expense, PaymentMethod = PaymentMethod.InstallmentCredit, Status = status, InstallmentNumber = firstNumber + index, Description = purchase.Description, CreatedAt = now, UpdatedAt = now }).ToArray();
    }
    private static MonthlySummaryResponseDto BuildMonthlySummary(DateOnly month, IEnumerable<FinancialTransaction> transactions)
    {
        var values = transactions.ToArray();
        var confirmedIncome = values.Where(x => x.Status == TransactionStatus.Confirmed && x.Type == FinancialCategoryType.Income).Sum(x => x.Amount);
        var confirmedExpense = values.Where(x => x.Status == TransactionStatus.Confirmed && x.Type == FinancialCategoryType.Expense).Sum(x => x.Amount);
        var projectedIncome = values.Where(x => x.Type == FinancialCategoryType.Income).Sum(x => x.Amount);
        var projectedExpense = values.Where(x => x.Type == FinancialCategoryType.Expense).Sum(x => x.Amount);
        return new() { Month = month, ConfirmedIncome = confirmedIncome, ConfirmedExpense = confirmedExpense, ConfirmedBalance = confirmedIncome - confirmedExpense, ProjectedIncome = projectedIncome, ProjectedExpense = projectedExpense, ProjectedBalance = projectedIncome - projectedExpense };
    }
    private static TransactionStatus EffectiveStatus(FinancialTransaction transaction) => transaction.Status == TransactionStatus.Planned && transaction.TransactionDate < LocalToday() ? TransactionStatus.Overdue : transaction.Status;
    private static BudgetAlert GetBudgetAlert(decimal? percentage) => percentage switch { null => BudgetAlert.None, > 100m => BudgetAlert.Exceeded, >= 100m => BudgetAlert.AtLimit, >= 80m => BudgetAlert.EightyPercent, _ => BudgetAlert.None };
    private static DateOnly FirstDayOfMonth(DateOnly value) => new(value.Year, value.Month, 1);
    private static DateOnly FirstDayOfNextMonth(DateOnly value) => FirstDayOfMonth(value).AddMonths(1);
    private static DateOnly LastDayOfMonth(DateOnly value) => FirstDayOfMonth(value).AddMonths(1).AddDays(-1);
    private static DateOnly LocalToday() => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "America/Sao_Paulo"));
    private static string? TrimOrNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static FinancialCategoryResponseDto MapCategory(FinancialCategory value) => new() { Id = value.Id, Name = value.Name, Type = value.Type, Archived = value.Archived };
    private static TransactionResponseDto MapTransaction(FinancialTransaction value) => new() { Id = value.Id, CategoryId = value.CategoryId, Amount = value.Amount, TransactionDate = value.TransactionDate, Type = value.Type, PaymentMethod = value.PaymentMethod, Status = EffectiveStatus(value), InstallmentNumber = value.InstallmentNumber, InstallmentPurchaseId = value.InstallmentPurchaseId, RecurringTransactionId = value.RecurringTransactionId, Description = value.Description };
    private static RecurringTransactionResponseDto MapRecurrence(RecurringTransaction value) => new() { Id = value.Id, CategoryId = value.CategoryId, Amount = value.Amount, Type = value.Type, PaymentMethod = value.PaymentMethod, FirstOccurrenceDate = value.FirstOccurrenceDate, EndedAt = value.EndedAt, Description = value.Description };
    private static InstallmentPurchaseResponseDto MapPurchase(InstallmentPurchase purchase, IEnumerable<FinancialTransaction> installments) => new() { Id = purchase.Id, CategoryId = purchase.CategoryId, TotalAmount = purchase.TotalAmount, InstallmentCount = purchase.InstallmentCount, Description = purchase.Description, Installments = installments.OrderBy(x => x.InstallmentNumber).Select(MapTransaction).ToArray() };
}
