using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceLifeOS.Application.Ports;
using ServiceLifeOS.Application.Services;
using ServiceLifeOS.Dtos.Finances;

namespace ServiceLifeOS.Api.Controllers;

[ApiController]
[Authorize]
[Route("finances")]
public sealed class FinancesController : ControllerBase
{
    private readonly FinanceService _financeService;
    private readonly ICurrentUser _currentUser;

    public FinancesController(FinanceService financeService, ICurrentUser currentUser)
    {
        _financeService = financeService;
        _currentUser = currentUser;
    }

    [HttpGet("categories")]
    public Task<IReadOnlyCollection<FinancialCategoryResponseDto>> GetCategories([FromQuery] bool includeArchived, CancellationToken cancellationToken) => _financeService.GetCategoriesAsync(_currentUser.UserId, includeArchived, cancellationToken);
    [HttpPost("categories")]
    public async Task<ActionResult<FinancialCategoryResponseDto>> CreateCategory(FinancialCategoryRequestDto request, CancellationToken cancellationToken) => Created(string.Empty, await _financeService.CreateCategoryAsync(_currentUser.UserId, request, cancellationToken));
    [HttpPut("categories/{categoryId:guid}")]
    public Task<FinancialCategoryResponseDto> UpdateCategory(Guid categoryId, FinancialCategoryRequestDto request, CancellationToken cancellationToken) => _financeService.UpdateCategoryAsync(_currentUser.UserId, categoryId, request, cancellationToken);
    [HttpDelete("categories/{categoryId:guid}")]
    public async Task<IActionResult> ArchiveCategory(Guid categoryId, CancellationToken cancellationToken) { await _financeService.ArchiveCategoryAsync(_currentUser.UserId, categoryId, cancellationToken); return NoContent(); }
    [HttpGet("categories/{categoryId:guid}/budget")]
    public Task<BudgetResponseDto?> GetBudget(Guid categoryId, [FromQuery] DateOnly? month, CancellationToken cancellationToken) => _financeService.GetBudgetAsync(_currentUser.UserId, categoryId, month, cancellationToken);
    [HttpPut("categories/{categoryId:guid}/budget")]
    public Task<BudgetResponseDto> SetBudget(Guid categoryId, BudgetRequestDto request, CancellationToken cancellationToken) => _financeService.SetBudgetAsync(_currentUser.UserId, categoryId, request, cancellationToken);
    [HttpPut("categories/{categoryId:guid}/budget-overrides/{month}")]
    public Task<BudgetResponseDto> SetBudgetOverride(Guid categoryId, DateOnly month, BudgetOverrideRequestDto request, CancellationToken cancellationToken) => _financeService.SetBudgetOverrideAsync(_currentUser.UserId, categoryId, month, request, cancellationToken);
    [HttpDelete("categories/{categoryId:guid}/budget-overrides/{month}")]
    public async Task<IActionResult> DeleteBudgetOverride(Guid categoryId, DateOnly month, CancellationToken cancellationToken) { await _financeService.DeleteBudgetOverrideAsync(_currentUser.UserId, categoryId, month, cancellationToken); return NoContent(); }
    [HttpGet("transactions")]
    public Task<PagedTransactionResponseDto> GetTransactions([FromQuery] TransactionQueryDto query, CancellationToken cancellationToken) => _financeService.GetTransactionsAsync(_currentUser.UserId, query, cancellationToken);
    [HttpPost("transactions")]
    public async Task<ActionResult<TransactionResponseDto>> CreateTransaction(TransactionRequestDto request, CancellationToken cancellationToken) => Created(string.Empty, await _financeService.CreateTransactionAsync(_currentUser.UserId, request, cancellationToken));
    [HttpPut("transactions/{transactionId:guid}")]
    public Task<TransactionResponseDto> UpdateTransaction(Guid transactionId, TransactionRequestDto request, CancellationToken cancellationToken) => _financeService.UpdateTransactionAsync(_currentUser.UserId, transactionId, request, cancellationToken);
    [HttpPost("transactions/{transactionId:guid}/confirm")]
    public Task<TransactionResponseDto> ConfirmTransaction(Guid transactionId, CancellationToken cancellationToken) => _financeService.ConfirmTransactionAsync(_currentUser.UserId, transactionId, cancellationToken);
    [HttpDelete("transactions/{transactionId:guid}")]
    public async Task<IActionResult> DeleteTransaction(Guid transactionId, CancellationToken cancellationToken) { await _financeService.DeleteTransactionAsync(_currentUser.UserId, transactionId, cancellationToken); return NoContent(); }
    [HttpGet("recurrences")]
    public Task<IReadOnlyCollection<RecurringTransactionResponseDto>> GetRecurrences(CancellationToken cancellationToken) => _financeService.GetRecurringTransactionsAsync(_currentUser.UserId, cancellationToken);
    [HttpPost("recurrences")]
    public async Task<ActionResult<RecurringTransactionResponseDto>> CreateRecurrence(RecurringTransactionRequestDto request, CancellationToken cancellationToken) => Created(string.Empty, await _financeService.CreateRecurringTransactionAsync(_currentUser.UserId, request, cancellationToken));
    [HttpPut("recurrences/{recurrenceId:guid}")]
    public Task<RecurringTransactionResponseDto> UpdateRecurrence(Guid recurrenceId, RecurringTransactionRequestDto request, CancellationToken cancellationToken) => _financeService.UpdateRecurringTransactionAsync(_currentUser.UserId, recurrenceId, request, cancellationToken);
    [HttpPost("recurrences/{recurrenceId:guid}/end")]
    public async Task<IActionResult> EndRecurrence(Guid recurrenceId, CancellationToken cancellationToken) { await _financeService.EndRecurringTransactionAsync(_currentUser.UserId, recurrenceId, cancellationToken); return NoContent(); }
    [HttpPost("installment-purchases")]
    public async Task<ActionResult<InstallmentPurchaseResponseDto>> CreateInstallmentPurchase(InstallmentPurchaseRequestDto request, CancellationToken cancellationToken) => Created(string.Empty, await _financeService.CreateInstallmentPurchaseAsync(_currentUser.UserId, request, cancellationToken));
    [HttpGet("installment-purchases/{purchaseId:guid}")]
    public Task<InstallmentPurchaseResponseDto> GetInstallmentPurchase(Guid purchaseId, CancellationToken cancellationToken) => _financeService.GetInstallmentPurchaseAsync(_currentUser.UserId, purchaseId, cancellationToken);
    [HttpPut("installment-purchases/{purchaseId:guid}")]
    public Task<InstallmentPurchaseResponseDto> UpdateInstallmentPurchase(Guid purchaseId, InstallmentPurchaseRequestDto request, CancellationToken cancellationToken) => _financeService.UpdateInstallmentPurchaseAsync(_currentUser.UserId, purchaseId, request, cancellationToken);
    [HttpGet("reports/monthly-summary")]
    public Task<MonthlySummaryResponseDto> GetMonthlySummary([FromQuery] DateOnly month, CancellationToken cancellationToken) => _financeService.GetMonthlySummaryAsync(_currentUser.UserId, month, cancellationToken);
    [HttpGet("reports/monthly-comparison")]
    public Task<MonthlyComparisonResponseDto> GetMonthlyComparison([FromQuery] DateOnly from, [FromQuery] DateOnly to, CancellationToken cancellationToken) => _financeService.GetMonthlyComparisonAsync(_currentUser.UserId, from, to, cancellationToken);
    [HttpGet("reports/cash-flow-projection")]
    public Task<MonthlyComparisonResponseDto> GetCashFlowProjection([FromQuery] DateOnly from, [FromQuery] DateOnly to, CancellationToken cancellationToken) => _financeService.GetCashFlowProjectionAsync(_currentUser.UserId, from, to, cancellationToken);
    [HttpGet("reports/category-spending")]
    public Task<IReadOnlyCollection<CategorySpendingResponseDto>> GetCategorySpending([FromQuery] DateOnly month, CancellationToken cancellationToken) => _financeService.GetCategorySpendingAsync(_currentUser.UserId, month, cancellationToken);
}
