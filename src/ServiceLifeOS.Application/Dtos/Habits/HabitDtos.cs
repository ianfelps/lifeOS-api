using ServiceLifeOS.Domain.Entities;

namespace ServiceLifeOS.Dtos.Habits;

public sealed class HabitScheduleRequestDto
{
    public HabitScheduleType Type { get; set; }
    public int TargetCount { get; set; } = 1;
    public IReadOnlyCollection<DayOfWeek> Weekdays { get; set; } = [];
}

public sealed class HabitRequestDto
{
    public string Title { get; set; } = string.Empty;
    public HabitPriority Priority { get; set; }
    public HabitScheduleRequestDto Schedule { get; set; } = new();
}

public sealed class HabitScheduleResponseDto
{
    public HabitScheduleType Type { get; set; }
    public int TargetCount { get; set; }
    public IReadOnlyCollection<DayOfWeek> Weekdays { get; set; } = [];
}

public sealed class HabitResponseDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public HabitPriority Priority { get; set; }
    public HabitStatus Status { get; set; }
    public HabitScheduleResponseDto Schedule { get; set; } = new();
}

public sealed class HabitQueryDto
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public bool IncludeArchived { get; set; }
    public HabitStatus? Status { get; set; }
}

public sealed class PagedHabitResponseDto
{
    public IReadOnlyCollection<HabitResponseDto> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
}

public sealed class HabitCompletionRequestDto
{
    public DateOnly CompletedOn { get; set; }
}

public sealed class HabitCompletionResponseDto
{
    public Guid Id { get; set; }
    public Guid HabitId { get; set; }
    public DateOnly CompletedOn { get; set; }
}

public sealed class HabitProgressResponseDto
{
    public Guid HabitId { get; set; }
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public int CompletionCount { get; set; }
    public int TargetCount { get; set; }
    public bool IsCompleted { get; set; }
    public int Streak { get; set; }
}
