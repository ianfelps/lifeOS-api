using ServiceLifeOS.Domain.Entities;

namespace ServiceLifeOS.Dtos.Workouts;

public sealed class ExerciseRequestDto
{
    public string Name { get; set; } = string.Empty;
}

public sealed class ExerciseResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool Archived { get; set; }
}

public sealed class WorkoutSheetSetRequestDto
{
    public int TargetRepetitions { get; set; }
}

public sealed class WorkoutSheetExerciseRequestDto
{
    public Guid ExerciseId { get; set; }
    public IReadOnlyCollection<WorkoutSheetSetRequestDto> Sets { get; set; } = [];
}

public sealed class WorkoutSheetRequestDto
{
    public string Name { get; set; } = string.Empty;
    public IReadOnlyCollection<WorkoutSheetExerciseRequestDto> Exercises { get; set; } = [];
}

public sealed class WorkoutSheetSetResponseDto
{
    public Guid Id { get; set; }
    public int Position { get; set; }
    public int TargetRepetitions { get; set; }
}

public sealed class WorkoutSheetExerciseResponseDto
{
    public Guid Id { get; set; }
    public Guid ExerciseId { get; set; }
    public string ExerciseName { get; set; } = string.Empty;
    public int Position { get; set; }
    public IReadOnlyCollection<WorkoutSheetSetResponseDto> Sets { get; set; } = [];
}

public sealed class WorkoutSheetResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool Archived { get; set; }
    public IReadOnlyCollection<WorkoutSheetExerciseResponseDto> Exercises { get; set; } = [];
}

public sealed class WorkoutSessionSetRequestDto
{
    public decimal? Weight { get; set; }
    public WeightUnit? WeightUnit { get; set; }
    public int? Repetitions { get; set; }
}

public sealed class WorkoutSessionExerciseRequestDto
{
    public Guid? ExerciseId { get; set; }
    public string? ExerciseName { get; set; }
    public IReadOnlyCollection<WorkoutSessionSetRequestDto> Sets { get; set; } = [];
}

public sealed class StartWorkoutSessionRequestDto
{
    public Guid? WorkoutSheetId { get; set; }
    public IReadOnlyCollection<WorkoutSessionExerciseRequestDto> Exercises { get; set; } = [];
}

public sealed class UpdateWorkoutSessionRequestDto
{
    public IReadOnlyCollection<WorkoutSessionExerciseRequestDto> Exercises { get; set; } = [];
}

public sealed class WorkoutSessionSetResponseDto
{
    public Guid Id { get; set; }
    public int Position { get; set; }
    public decimal? Weight { get; set; }
    public WeightUnit? WeightUnit { get; set; }
    public int? Repetitions { get; set; }
}

public sealed class WorkoutSessionExerciseResponseDto
{
    public Guid Id { get; set; }
    public Guid? ExerciseId { get; set; }
    public string ExerciseName { get; set; } = string.Empty;
    public int Position { get; set; }
    public IReadOnlyCollection<WorkoutSessionSetResponseDto> Sets { get; set; } = [];
}

public sealed class WorkoutSessionResponseDto
{
    public Guid Id { get; set; }
    public Guid? WorkoutSheetId { get; set; }
    public WorkoutSessionStatus Status { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public IReadOnlyCollection<WorkoutSessionExerciseResponseDto> Exercises { get; set; } = [];
}

public sealed class WorkoutSessionQueryDto
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public Guid? WorkoutSheetId { get; set; }
    public WorkoutSessionStatus? Status { get; set; }
}

public sealed class PagedWorkoutSessionResponseDto
{
    public IReadOnlyCollection<WorkoutSessionResponseDto> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
}

public sealed class ExerciseProgressItemDto
{
    public Guid SessionId { get; set; }
    public DateTime CompletedAt { get; set; }
    public WeightUnit WeightUnit { get; set; }
    public decimal MaxWeight { get; set; }
    public decimal BestSetWeight { get; set; }
    public int BestSetRepetitions { get; set; }
    public decimal TotalVolume { get; set; }
}

public sealed class ExerciseProgressResponseDto
{
    public Guid ExerciseId { get; set; }
    public string ExerciseName { get; set; } = string.Empty;
    public IReadOnlyCollection<ExerciseProgressItemDto> Items { get; set; } = [];
}
