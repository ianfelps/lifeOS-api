using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ServiceLifeOS.Application.Ports;
using ServiceLifeOS.Application.Services;
using ServiceLifeOS.Dtos.Auth;

namespace ServiceLifeOS.Api.Controllers;

[ApiController]
[Route("auth")]
public sealed class AuthController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly ICurrentUser _currentUser;

    public AuthController(
        AuthService authService,
        ICurrentUser currentUser)
    {
        _authService = authService;
        _currentUser = currentUser;
    }

    [HttpPost("login")]
    [EnableRateLimiting("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(
        LoginRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _authService.LoginAsync(request, cancellationToken));
        }
        catch (UnauthorizedAccessException exception)
        {
            return Unauthorized(new { message = exception.Message });
        }
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<MeResponseDto>> Me(CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _authService.GetMeAsync(_currentUser.UserId, cancellationToken));
        }
        catch (UnauthorizedAccessException exception)
        {
            return Unauthorized(new { message = exception.Message });
        }
    }
}
