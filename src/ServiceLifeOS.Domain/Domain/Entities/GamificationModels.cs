namespace ServiceLifeOS.Domain.Entities;

public sealed class Goal
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public GoalType Type { get; set; }
    public GoalStatus Status { get; set; } = GoalStatus.Active;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal TargetValue { get; set; }
    public string Unit { get; set; } = string.Empty;
    public DateOnly? DueDate { get; set; }
    public decimal? ManualProgress { get; set; }
    public bool Archived { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class GoalSourceLink
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GoalId { get; set; }
    public GoalSourceType SourceType { get; set; }
    public Guid SourceId { get; set; }
}

public sealed class XpEventRule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public XpEventType EventType { get; set; }
    public int Amount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class LevelProgressionRule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public int BaseXp { get; set; }
    public int IncrementPerLevel { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class Badge
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool Archived { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class BadgeCriterion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BadgeId { get; set; }
    public BadgeCriterionType Type { get; set; }
    public decimal TargetValue { get; set; }
    public Guid? HabitId { get; set; }
    public Guid? ExerciseId { get; set; }
    public Guid? FinancialCategoryId { get; set; }
    public Guid? GoalId { get; set; }
}

public sealed class UserBadge
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public Guid BadgeId { get; set; }
    public DateTime UnlockedAt { get; set; }
}

public sealed class XpLedgerEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public XpLedgerEntryType Type { get; set; }
    public int Amount { get; set; }
    public XpEventType? EventType { get; set; }
    public string? SourceType { get; set; }
    public Guid? SourceId { get; set; }
    public Guid? ReversedEntryId { get; set; }
    public DateTime CreatedAt { get; set; }
}
