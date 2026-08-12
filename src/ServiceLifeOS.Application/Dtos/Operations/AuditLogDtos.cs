using ServiceLifeOS.Domain.Entities;

namespace ServiceLifeOS.Dtos.Operations;

public sealed class AuditLogQueryDto
{
    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 50;

    public AuditAction? Action { get; set; }

    public string? ResourceType { get; set; }

    public DateTime? CreatedFrom { get; set; }

    public DateTime? CreatedTo { get; set; }
}

public sealed class AuditLogResponseDto
{
    public Guid Id { get; set; }

    public AuditAction Action { get; set; }

    public string ResourceType { get; set; } = string.Empty;

    public Guid? ResourceId { get; set; }

    public string? PreviousValues { get; set; }

    public string? CurrentValues { get; set; }

    public DateTime CreatedAt { get; set; }
}

public sealed class PagedAuditLogResponseDto
{
    public IReadOnlyCollection<AuditLogResponseDto> Items { get; set; } = [];

    public int Page { get; set; }

    public int PageSize { get; set; }

    public int TotalCount { get; set; }
}
