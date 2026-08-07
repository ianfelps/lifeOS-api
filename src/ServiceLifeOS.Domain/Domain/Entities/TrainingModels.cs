namespace ServiceLifeOS.Domain.Entities;

public sealed class Exercise
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool Archived { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class WorkoutSheet
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool Archived { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class WorkoutSheetExercise
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WorkoutSheetId { get; set; }
    public Guid ExerciseId { get; set; }
    public int Position { get; set; }
}

public sealed class WorkoutSheetExerciseSet
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WorkoutSheetExerciseId { get; set; }
    public int Position { get; set; }
    public int TargetRepetitions { get; set; }
}

public sealed class WorkoutSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public Guid? WorkoutSheetId { get; set; }
    public WorkoutSessionStatus Status { get; set; } = WorkoutSessionStatus.Draft;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class WorkoutSessionExercise
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WorkoutSessionId { get; set; }
    public Guid? ExerciseId { get; set; }
    public string ExerciseName { get; set; } = string.Empty;
    public int Position { get; set; }
}

public sealed class WorkoutSessionSet
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WorkoutSessionExerciseId { get; set; }
    public int Position { get; set; }
    public decimal? Weight { get; set; }
    public WeightUnit? WeightUnit { get; set; }
    public int? Repetitions { get; set; }
}
