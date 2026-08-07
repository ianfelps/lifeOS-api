using ServiceLifeOS.Application.Ports;
using ServiceLifeOS.Dtos.Auth;

namespace ServiceLifeOS.Application.Services;

public sealed class AuthService
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;

    public AuthService(
        IUserRepository users,
        IPasswordHasher passwordHasher,
        ITokenService tokenService)
    {
        _users = users;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    public async Task<AuthResponseDto> LoginAsync(
        LoginRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var userName = request.UserName?.Trim();
        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(request.Password))
        {
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        var user = await _users.GetActiveByUserNameAsync(userName, cancellationToken);
        if (user is null || !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        return new AuthResponseDto
        {
            AccessToken = _tokenService.CreateAccessToken(
                user.Id,
                user.UserName,
                user.DisplayName),
            User = new MeResponseDto
            {
                UserId = user.Id,
                UserName = user.UserName,
                DisplayName = user.DisplayName
            }
        };
    }

    public async Task<MeResponseDto> GetMeAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _users.GetActiveByIdAsync(userId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Authenticated user was not found.");
        return new MeResponseDto
        {
            UserId = user.Id,
            UserName = user.UserName,
            DisplayName = user.DisplayName
        };
    }
}
