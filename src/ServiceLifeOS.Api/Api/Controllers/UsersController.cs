using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceLifeOS.Application.Ports;
using ServiceLifeOS.Application.Services;
using ServiceLifeOS.Dtos.Users;

namespace ServiceLifeOS.Api.Controllers;

[ApiController]
[Authorize]
[Route("users/me")]
public sealed class UsersController : ControllerBase
{
    private readonly UserService _userService;
    private readonly ICurrentUser _currentUser;

    public UsersController(UserService userService, ICurrentUser currentUser)
    {
        _userService = userService;
        _currentUser = currentUser;
    }

    [HttpPut("password")]
    public async Task<IActionResult> ChangePassword(
        ChangePasswordRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _userService.ChangePasswordAsync(
                _currentUser.UserId,
                _currentUser.TokenId,
                request,
                cancellationToken);
            return NoContent();
        }
        catch (UnauthorizedAccessException exception)
        {
            return Unauthorized(new { message = exception.Message });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpGet("preferences")]
    public async Task<ActionResult<UserPreferenceResponseDto>> GetPreferences(
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _userService.GetPreferenceAsync(
                _currentUser.UserId,
                cancellationToken));
        }
        catch (InvalidOperationException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }

    [HttpPut("preferences")]
    public async Task<ActionResult<UserPreferenceResponseDto>> UpdatePreferences(
        UpdateUserPreferenceRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _userService.UpdatePreferenceAsync(
                _currentUser.UserId,
                request,
                cancellationToken));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }

    [HttpDelete("sessions/others")]
    public async Task<ActionResult<RevokeOtherSessionsResponseDto>> RevokeOtherSessions(
        CancellationToken cancellationToken)
    {
        return Ok(await _userService.RevokeOtherSessionsAsync(
            _currentUser.UserId,
            _currentUser.TokenId,
            cancellationToken));
    }
}
