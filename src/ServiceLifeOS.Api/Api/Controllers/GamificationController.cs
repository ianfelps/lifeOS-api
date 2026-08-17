using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceLifeOS.Application.Ports;
using ServiceLifeOS.Application.Services;
using ServiceLifeOS.Dtos.Gamification;

namespace ServiceLifeOS.Api.Controllers;

[ApiController]
[Authorize]
[Route("gamification")]
public sealed class GamificationController : ControllerBase
{
    private readonly GamificationService _gamification;
    private readonly ICurrentUser _currentUser;

    public GamificationController(
        GamificationService gamification,
        ICurrentUser currentUser)
    {
        _gamification = gamification;
        _currentUser = currentUser;
    }

    [HttpGet("profile")]
    public Task<GamificationProfileResponseDto> GetProfile(CancellationToken cancellationToken)
    {
        return _gamification.GetProfileAsync(_currentUser.UserId, cancellationToken);
    }

    [HttpGet("ledger")]
    public Task<PagedXpLedgerResponseDto> GetLedger(
        [FromQuery] XpLedgerQueryDto query,
        CancellationToken cancellationToken)
    {
        return _gamification.GetLedgerAsync(_currentUser.UserId, query, cancellationToken);
    }

    [HttpGet("goals")]
    public Task<PagedGoalResponseDto> GetGoals(
        [FromQuery] GoalQueryDto query,
        CancellationToken cancellationToken)
    {
        return _gamification.GetGoalsAsync(_currentUser.UserId, query, cancellationToken);
    }

    [HttpPost("goals")]
    public async Task<ActionResult<GoalResponseDto>> CreateGoal(
        GoalRequestDto request,
        CancellationToken cancellationToken)
    {
        var goal = await _gamification.CreateGoalAsync(
            _currentUser.UserId,
            request,
            cancellationToken);

        return Created(string.Empty, goal);
    }

    [HttpGet("goals/{goalId:guid}")]
    public Task<GoalResponseDto> GetGoal(Guid goalId, CancellationToken cancellationToken)
    {
        return _gamification.GetGoalAsync(_currentUser.UserId, goalId, cancellationToken);
    }

    [HttpPut("goals/{goalId:guid}")]
    public Task<GoalResponseDto> UpdateGoal(
        Guid goalId,
        GoalRequestDto request,
        CancellationToken cancellationToken)
    {
        return _gamification.UpdateGoalAsync(_currentUser.UserId, goalId, request, cancellationToken);
    }

    [HttpPut("goals/{goalId:guid}/progress")]
    public Task<GoalResponseDto> UpdateProgress(
        Guid goalId,
        ManualGoalProgressRequestDto request,
        CancellationToken cancellationToken)
    {
        return _gamification.UpdateManualProgressAsync(
            _currentUser.UserId,
            goalId,
            request,
            cancellationToken);
    }

    [HttpPost("goals/{goalId:guid}/cancel")]
    public async Task<IActionResult> CancelGoal(Guid goalId, CancellationToken cancellationToken)
    {
        await _gamification.CancelGoalAsync(_currentUser.UserId, goalId, cancellationToken);
        return NoContent();
    }

    [HttpDelete("goals/{goalId:guid}")]
    public async Task<IActionResult> ArchiveGoal(Guid goalId, CancellationToken cancellationToken)
    {
        await _gamification.ArchiveGoalAsync(_currentUser.UserId, goalId, cancellationToken);
        return NoContent();
    }

    [HttpGet("xp-rules")]
    public Task<IReadOnlyCollection<XpEventRuleRequestDto>> GetXpRules(CancellationToken cancellationToken)
    {
        return _gamification.GetXpRulesAsync(_currentUser.UserId, cancellationToken);
    }

    [HttpPut("xp-rules")]
    public Task<IReadOnlyCollection<XpEventRuleRequestDto>> UpdateXpRules(
        IReadOnlyCollection<XpEventRuleRequestDto> request,
        CancellationToken cancellationToken)
    {
        return _gamification.UpdateXpRulesAsync(_currentUser.UserId, request, cancellationToken);
    }

    [HttpGet("level-progression")]
    public Task<LevelProgressionRuleRequestDto> GetLevelProgression(CancellationToken cancellationToken)
    {
        return _gamification.GetLevelRuleAsync(_currentUser.UserId, cancellationToken);
    }

    [HttpPut("level-progression")]
    public Task<LevelProgressionRuleRequestDto> UpdateLevelProgression(
        LevelProgressionRuleRequestDto request,
        CancellationToken cancellationToken)
    {
        return _gamification.UpdateLevelRuleAsync(_currentUser.UserId, request, cancellationToken);
    }

    [HttpGet("badges")]
    public Task<IReadOnlyCollection<BadgeResponseDto>> GetBadges(
        [FromQuery] bool includeArchived,
        CancellationToken cancellationToken)
    {
        return _gamification.GetBadgesAsync(_currentUser.UserId, includeArchived, cancellationToken);
    }

    [HttpPost("badges")]
    public async Task<ActionResult<BadgeResponseDto>> CreateBadge(
        BadgeRequestDto request,
        CancellationToken cancellationToken)
    {
        var badge = await _gamification.CreateBadgeAsync(
            _currentUser.UserId,
            request,
            cancellationToken);

        return Created(string.Empty, badge);
    }

    [HttpPut("badges/{badgeId:guid}")]
    public Task<BadgeResponseDto> UpdateBadge(
        Guid badgeId,
        BadgeRequestDto request,
        CancellationToken cancellationToken)
    {
        return _gamification.UpdateBadgeAsync(_currentUser.UserId, badgeId, request, cancellationToken);
    }

    [HttpDelete("badges/{badgeId:guid}")]
    public async Task<IActionResult> ArchiveBadge(Guid badgeId, CancellationToken cancellationToken)
    {
        await _gamification.ArchiveBadgeAsync(_currentUser.UserId, badgeId, cancellationToken);
        return NoContent();
    }
}
