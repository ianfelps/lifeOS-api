using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceLifeOS.Application.Ports;
using ServiceLifeOS.Application.Services;
using ServiceLifeOS.Dtos.Habits;

namespace ServiceLifeOS.Api.Controllers;

[ApiController]
[Authorize]
[Route("habits")]
public sealed class HabitsController : ControllerBase
{
    private readonly HabitService _habitService;
    private readonly ICurrentUser _currentUser;

    public HabitsController(HabitService habitService, ICurrentUser currentUser)
    {
        _habitService = habitService;
        _currentUser = currentUser;
    }

    [HttpGet]
    public Task<PagedHabitResponseDto> GetHabits(
        [FromQuery] HabitQueryDto query,
        CancellationToken cancellationToken) =>
        _habitService.GetHabitsAsync(_currentUser.UserId, query, cancellationToken);

    [HttpPost]
    public async Task<ActionResult<HabitResponseDto>> CreateHabit(
        HabitRequestDto request,
        CancellationToken cancellationToken) =>
        Created(
            string.Empty,
            await _habitService.CreateHabitAsync(_currentUser.UserId, request, cancellationToken));

    [HttpGet("{habitId:guid}")]
    public Task<HabitResponseDto> GetHabit(Guid habitId, CancellationToken cancellationToken) =>
        _habitService.GetHabitAsync(_currentUser.UserId, habitId, cancellationToken);

    [HttpPut("{habitId:guid}")]
    public Task<HabitResponseDto> UpdateHabit(
        Guid habitId,
        HabitRequestDto request,
        CancellationToken cancellationToken) =>
        _habitService.UpdateHabitAsync(_currentUser.UserId, habitId, request, cancellationToken);

    [HttpPost("{habitId:guid}/pause")]
    public async Task<IActionResult> PauseHabit(Guid habitId, CancellationToken cancellationToken)
    {
        await _habitService.PauseHabitAsync(_currentUser.UserId, habitId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{habitId:guid}/resume")]
    public async Task<IActionResult> ResumeHabit(Guid habitId, CancellationToken cancellationToken)
    {
        await _habitService.ResumeHabitAsync(_currentUser.UserId, habitId, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{habitId:guid}")]
    public async Task<IActionResult> ArchiveHabit(Guid habitId, CancellationToken cancellationToken)
    {
        await _habitService.ArchiveHabitAsync(_currentUser.UserId, habitId, cancellationToken);
        return NoContent();
    }

    [HttpGet("{habitId:guid}/completions")]
    public Task<IReadOnlyCollection<HabitCompletionResponseDto>> GetCompletions(
        Guid habitId,
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken cancellationToken) =>
        _habitService.GetCompletionsAsync(_currentUser.UserId, habitId, from, to, cancellationToken);

    [HttpPost("{habitId:guid}/completions")]
    public async Task<ActionResult<HabitCompletionResponseDto>> CreateCompletion(
        Guid habitId,
        HabitCompletionRequestDto request,
        CancellationToken cancellationToken) =>
        Created(
            string.Empty,
            await _habitService.CreateCompletionAsync(
                _currentUser.UserId,
                habitId,
                request,
                cancellationToken));

    [HttpDelete("{habitId:guid}/completions/{completionId:guid}")]
    public async Task<IActionResult> DeleteCompletion(
        Guid habitId,
        Guid completionId,
        CancellationToken cancellationToken)
    {
        await _habitService.DeleteCompletionAsync(
            _currentUser.UserId,
            habitId,
            completionId,
            cancellationToken);
        return NoContent();
    }

    [HttpGet("{habitId:guid}/progress")]
    public Task<HabitProgressResponseDto> GetProgress(
        Guid habitId,
        [FromQuery] DateOnly date,
        CancellationToken cancellationToken) =>
        _habitService.GetProgressAsync(_currentUser.UserId, habitId, date, cancellationToken);

    [HttpGet("pending")]
    public Task<IReadOnlyCollection<HabitProgressResponseDto>> GetPendingHabits(
        [FromQuery] DateOnly date,
        CancellationToken cancellationToken) =>
        _habitService.GetPendingHabitsAsync(_currentUser.UserId, date, cancellationToken);
}
