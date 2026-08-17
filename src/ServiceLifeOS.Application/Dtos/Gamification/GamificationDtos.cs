using ServiceLifeOS.Domain.Entities;

namespace ServiceLifeOS.Dtos.Gamification;

public sealed class GamificationProfileResponseDto
{
    public int TotalXp { get; set; }
    public int Level { get; set; }
    public int CurrentLevelXp { get; set; }
    public int? NextLevelXp { get; set; }
    public IReadOnlyCollection<BadgeResponseDto> Badges { get; set; } = [];
}

public sealed class XpLedgerQueryDto
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public XpEventType? EventType { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
}

public sealed class XpLedgerEntryResponseDto
{
    public Guid Id { get; set; }
    public XpLedgerEntryType Type { get; set; }
    public int Amount { get; set; }
    public XpEventType? EventType { get; set; }
    public string? SourceType { get; set; }
    public Guid? SourceId { get; set; }
    public Guid? ReversedEntryId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class PagedXpLedgerResponseDto
{
    public IReadOnlyCollection<XpLedgerEntryResponseDto> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
}

public sealed class GoalSourceRequestDto
{
    public GoalSourceType SourceType { get; set; }
    public Guid SourceId { get; set; }
}

public sealed class GoalRequestDto
{
    public GoalType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal TargetValue { get; set; }
    public string Unit { get; set; } = string.Empty;
    public DateOnly? DueDate { get; set; }
    public IReadOnlyCollection<GoalSourceRequestDto> Sources { get; set; } = [];
}

public sealed class ManualGoalProgressRequestDto
{
    public decimal Progress { get; set; }
}

public sealed class GoalResponseDto
{
    public Guid Id { get; set; }
    public GoalType Type { get; set; }
    public GoalStatus Status { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal TargetValue { get; set; }
    public decimal Progress { get; set; }
    public string Unit { get; set; } = string.Empty;
    public DateOnly? DueDate { get; set; }
    public bool Archived { get; set; }
    public IReadOnlyCollection<GoalSourceRequestDto> Sources { get; set; } = [];
}

public sealed class GoalQueryDto
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public bool IncludeArchived { get; set; }
    public GoalStatus? Status { get; set; }
}

public sealed class PagedGoalResponseDto
{
    public IReadOnlyCollection<GoalResponseDto> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
}

public sealed class XpEventRuleRequestDto
{
    public XpEventType EventType { get; set; }
    public int Amount { get; set; }
}

public sealed class LevelProgressionRuleRequestDto
{
    public int BaseXp { get; set; }
    public int IncrementPerLevel { get; set; }
}

public sealed class BadgeCriterionRequestDto
{
    public BadgeCriterionType Type { get; set; }
    public decimal TargetValue { get; set; }
    public Guid? HabitId { get; set; }
    public Guid? ExerciseId { get; set; }
    public Guid? FinancialCategoryId { get; set; }
    public Guid? GoalId { get; set; }
}

public sealed class BadgeRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public IReadOnlyCollection<BadgeCriterionRequestDto> Criteria { get; set; } = [];
}

public sealed class BadgeResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool Archived { get; set; }
    public DateTime? UnlockedAt { get; set; }
    public IReadOnlyCollection<BadgeCriterionRequestDto> Criteria { get; set; } = [];
}
