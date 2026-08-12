using ServiceLifeOS.Application.Ports;
using ServiceLifeOS.Dtos.Auth;

namespace ServiceLifeOS.Application.Services;

public sealed class AuthService
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IUserSessionRepository _sessions;
    private readonly IAuditLogRepository _auditLogs;
    private readonly IUnitOfWork _unitOfWork;

    public AuthService(
        IUserRepository users,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IUserSessionRepository sessions,
        IAuditLogRepository auditLogs,
        IUnitOfWork unitOfWork)
    {
        _users = users;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _sessions = sessions;
        _auditLogs = auditLogs;
        _unitOfWork = unitOfWork;
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

        var now = DateTime.UtcNow;
        var accessToken = _tokenService.CreateAccessToken(
            user.Id,
            user.UserName,
            user.DisplayName);
        await _sessions.CreateAsync(new()
        {
            UserId = user.Id,
            TokenId = accessToken.TokenId,
            CreatedAt = now,
            ExpiresAt = accessToken.ExpiresAt,
            LastUsedAt = now
        }, cancellationToken);
        await _auditLogs.CreateAsync(new()
        {
            UserId = user.Id,
            Action = ServiceLifeOS.Domain.Entities.AuditAction.Login,
            ResourceType = "User",
            CreatedAt = now
        }, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResponseDto
        {
            AccessToken = accessToken.Value,
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
