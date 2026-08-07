namespace ServiceLifeOS.Domain.Entities;

public sealed class Habit
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public HabitPriority Priority { get; set; }
    public HabitStatus Status { get; set; } = HabitStatus.Active;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class HabitSchedule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid HabitId { get; set; }
    public HabitScheduleType Type { get; set; }
    public int TargetCount { get; set; } = 1;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class HabitScheduleWeekday
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid HabitScheduleId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
}

public sealed class HabitCompletion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public Guid HabitId { get; set; }
    public DateOnly CompletedOn { get; set; }
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
