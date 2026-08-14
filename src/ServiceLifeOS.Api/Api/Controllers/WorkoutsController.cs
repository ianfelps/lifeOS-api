using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceLifeOS.Application.Ports;
using ServiceLifeOS.Application.Services;
using ServiceLifeOS.Dtos.Workouts;

namespace ServiceLifeOS.Api.Controllers;

[ApiController]
[Authorize]
[Route("workouts")]
public sealed class WorkoutsController : ControllerBase
{
    private readonly WorkoutService _workoutService;
    private readonly ICurrentUser _currentUser;
    public WorkoutsController(WorkoutService workoutService, ICurrentUser currentUser)
    {
        _workoutService = workoutService;
        _currentUser = currentUser;
    }

    [HttpGet("exercises")]
    public Task<IReadOnlyCollection<ExerciseResponseDto>> GetExercises(
        [FromQuery] bool includeArchived,
        CancellationToken cancellationToken)
    {
        return _workoutService.GetExercisesAsync(_currentUser.UserId, includeArchived, cancellationToken);
    }

    [HttpPost("exercises")]
    public async Task<ActionResult<ExerciseResponseDto>> CreateExercise(
        ExerciseRequestDto request,
        CancellationToken cancellationToken)
    {
        var exercise = await _workoutService.CreateExerciseAsync(_currentUser.UserId, request, cancellationToken);

        return Created(string.Empty, exercise);
    }

    [HttpPut("exercises/{exerciseId:guid}")]
    public Task<ExerciseResponseDto> UpdateExercise(
        Guid exerciseId,
        ExerciseRequestDto request,
        CancellationToken cancellationToken)
    {
        return _workoutService.UpdateExerciseAsync(_currentUser.UserId, exerciseId, request, cancellationToken);
    }

    [HttpDelete("exercises/{exerciseId:guid}")]
    public async Task<IActionResult> ArchiveExercise(Guid exerciseId, CancellationToken cancellationToken)
    {
        await _workoutService.ArchiveExerciseAsync(_currentUser.UserId, exerciseId, cancellationToken);

        return NoContent();
    }

    [HttpGet("sheets")]
    public Task<IReadOnlyCollection<WorkoutSheetResponseDto>> GetSheets(
        [FromQuery] bool includeArchived,
        CancellationToken cancellationToken)
    {
        return _workoutService.GetSheetsAsync(_currentUser.UserId, includeArchived, cancellationToken);
    }

    [HttpPost("sheets")]
    public async Task<ActionResult<WorkoutSheetResponseDto>> CreateSheet(
        WorkoutSheetRequestDto request,
        CancellationToken cancellationToken)
    {
        var sheet = await _workoutService.CreateSheetAsync(_currentUser.UserId, request, cancellationToken);

        return Created(string.Empty, sheet);
    }

    [HttpGet("sheets/{sheetId:guid}")]
    public Task<WorkoutSheetResponseDto> GetSheet(Guid sheetId, CancellationToken cancellationToken)
    {
        return _workoutService.GetSheetAsync(_currentUser.UserId, sheetId, cancellationToken);
    }

    [HttpPut("sheets/{sheetId:guid}")]
    public Task<WorkoutSheetResponseDto> UpdateSheet(
        Guid sheetId,
        WorkoutSheetRequestDto request,
        CancellationToken cancellationToken)
    {
        return _workoutService.UpdateSheetAsync(_currentUser.UserId, sheetId, request, cancellationToken);
    }

    [HttpDelete("sheets/{sheetId:guid}")]
    public async Task<IActionResult> ArchiveSheet(Guid sheetId, CancellationToken cancellationToken)
    {
        await _workoutService.ArchiveSheetAsync(_currentUser.UserId, sheetId, cancellationToken);

        return NoContent();
    }

    [HttpGet("sessions")]
    public Task<PagedWorkoutSessionResponseDto> GetSessions(
        [FromQuery] WorkoutSessionQueryDto query,
        CancellationToken cancellationToken)
    {
        return _workoutService.GetSessionsAsync(_currentUser.UserId, query, cancellationToken);
    }

    [HttpPost("sessions")]
    public async Task<ActionResult<WorkoutSessionResponseDto>> StartSession(
        StartWorkoutSessionRequestDto request,
        CancellationToken cancellationToken)
    {
        var session = await _workoutService.StartSessionAsync(_currentUser.UserId, request, cancellationToken);

        return Created(string.Empty, session);
    }

    [HttpGet("sessions/{sessionId:guid}")]
    public Task<WorkoutSessionResponseDto> GetSession(Guid sessionId, CancellationToken cancellationToken)
    {
        return _workoutService.GetSessionAsync(_currentUser.UserId, sessionId, cancellationToken);
    }

    [HttpPut("sessions/{sessionId:guid}")]
    public Task<WorkoutSessionResponseDto> UpdateSession(
        Guid sessionId,
        UpdateWorkoutSessionRequestDto request,
        CancellationToken cancellationToken)
    {
        return _workoutService.UpdateSessionAsync(_currentUser.UserId, sessionId, request, cancellationToken);
    }

    [HttpPost("sessions/{sessionId:guid}/complete")]
    public Task<WorkoutSessionResponseDto> CompleteSession(Guid sessionId, CancellationToken cancellationToken)
    {
        return _workoutService.CompleteSessionAsync(_currentUser.UserId, sessionId, cancellationToken);
    }

    [HttpPost("sessions/{sessionId:guid}/cancel")]
    public async Task<IActionResult> CancelSession(Guid sessionId, CancellationToken cancellationToken)
    {
        await _workoutService.CancelSessionAsync(_currentUser.UserId, sessionId, cancellationToken);

        return NoContent();
    }

    [HttpDelete("sessions/{sessionId:guid}")]
    public async Task<IActionResult> DeleteSession(Guid sessionId, CancellationToken cancellationToken)
    {
        await _workoutService.DeleteSessionAsync(_currentUser.UserId, sessionId, cancellationToken);

        return NoContent();
    }

    [HttpGet("progress/exercises/{exerciseId:guid}")]
    public Task<ExerciseProgressResponseDto> GetExerciseProgress(Guid exerciseId, CancellationToken cancellationToken)
    {
        return _workoutService.GetExerciseProgressAsync(_currentUser.UserId, exerciseId, cancellationToken);
    }
}
