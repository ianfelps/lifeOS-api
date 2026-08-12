using ServiceLifeOS.Application.Ports;
using ServiceLifeOS.Dtos.Operations;

namespace ServiceLifeOS.Application.Services;

public sealed class OperationsService
{
    private readonly IAuditLogRepository _auditLogs;

    public OperationsService(IAuditLogRepository auditLogs)
    {
        _auditLogs = auditLogs;
    }

    public async Task<PagedAuditLogResponseDto> GetAuditLogsAsync(
        string userId,
        AuditLogQueryDto query,
        CancellationToken cancellationToken = default)
    {
        if (query.Page < 1 || query.PageSize is < 1 or > 100)
        {
            throw new ArgumentException("Page must be at least one and page size must be between one and 100.");
        }
        if (query.CreatedFrom.HasValue && query.CreatedTo.HasValue && query.CreatedFrom > query.CreatedTo)
        {
            throw new ArgumentException("Created from must be before created to.");
        }

        var page = await _auditLogs.GetPageAsync(userId, new()
        {
            Page = query.Page,
            PageSize = query.PageSize,
            Action = query.Action,
            ResourceType = string.IsNullOrWhiteSpace(query.ResourceType) ? null : query.ResourceType.Trim(),
            CreatedFrom = query.CreatedFrom,
            CreatedTo = query.CreatedTo
        }, cancellationToken);
        return new()
        {
            Items = page.Items.Select(x => new AuditLogResponseDto
            {
                Id = x.Id,
                Action = x.Action,
                ResourceType = x.ResourceType,
                ResourceId = x.ResourceId,
                PreviousValues = x.PreviousValues,
                CurrentValues = x.CurrentValues,
                CreatedAt = x.CreatedAt
            }).ToArray(),
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = page.TotalCount
        };
    }
}
