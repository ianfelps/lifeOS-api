using ServiceLifeOS.Application.Ports;
using ServiceLifeOS.Application.Options;
using ServiceLifeOS.Domain.Entities;
using ServiceLifeOS.Dtos.Auth;

namespace ServiceLifeOS.Application.Services;

public sealed class AuthService
{
    private readonly IUserRepository _users;
    private readonly IInitialUserRegistrationRepository _initialUserRegistration;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IUserSessionRepository _sessions;
    private readonly IAuditLogRepository _auditLogs;
    private readonly IUnitOfWork _unitOfWork;
    private readonly PasswordPolicyOptions _passwordPolicy;

    public AuthService(
        IUserRepository users,
        IInitialUserRegistrationRepository initialUserRegistration,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IUserSessionRepository sessions,
        IAuditLogRepository auditLogs,
        IUnitOfWork unitOfWork,
        PasswordPolicyOptions? passwordPolicy = null)
    {
        _users = users;
        _initialUserRegistration = initialUserRegistration;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _sessions = sessions;
        _auditLogs = auditLogs;
        _unitOfWork = unitOfWork;
        _passwordPolicy = passwordPolicy ?? new PasswordPolicyOptions();
    }

    public async Task<MeResponseDto> RegisterInitialUserAsync(
        RegisterInitialUserRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var userName = request.UserName?.Trim();
        var displayName = request.DisplayName?.Trim();
        if (string.IsNullOrWhiteSpace(userName) ||
            string.IsNullOrWhiteSpace(displayName) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            throw new ArgumentException("User name, display name, and password are required.");
        }
        if (request.Password.Length < _passwordPolicy.MinimumLength)
        {
            throw new ArgumentException("Password does not meet the minimum length.");
        }

        var now = DateTime.UtcNow;
        var user = new AppUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = userName,
            DisplayName = displayName,
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            Active = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        if (!await _initialUserRegistration.CreateAsync(user, cancellationToken))
        {
            throw new InvalidOperationException("The initial user has already been registered.");
        }

        return new MeResponseDto
        {
            UserId = user.Id,
            UserName = user.UserName,
            DisplayName = user.DisplayName
        };
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
        var refreshToken = _tokenService.CreateRefreshToken();
        var session = new UserSession
        {
            UserId = user.Id,
            TokenId = accessToken.TokenId,
            CreatedAt = now,
            ExpiresAt = refreshToken.ExpiresAt,
            LastUsedAt = now
        };
        await _sessions.CreateAsync(
            session,
            new RefreshToken
            {
                UserSessionId = session.Id,
                TokenHash = refreshToken.Hash,
                CreatedAt = now,
                ExpiresAt = refreshToken.ExpiresAt
            },
            cancellationToken);
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
            RefreshToken = refreshToken.Value,
            ExpiresAt = accessToken.ExpiresAt,
            User = new MeResponseDto
            {
                UserId = user.Id,
                UserName = user.UserName,
                DisplayName = user.DisplayName
            }
        };
    }

    public async Task<AuthResponseDto> RefreshAsync(
        RefreshTokenRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            throw new UnauthorizedAccessException("Refresh token is required.");
        }

        var now = DateTime.UtcNow;
        var current = await _sessions.GetRefreshTokenSessionAsync(
            _tokenService.HashRefreshToken(request.RefreshToken),
            cancellationToken);
        if (current is null)
        {
            throw new UnauthorizedAccessException("Refresh token is invalid.");
        }
        if (current.RefreshToken.UsedAt.HasValue)
        {
            await _sessions.RevokeSessionAsync(current.Session.Id, now, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            throw new UnauthorizedAccessException("Refresh token reuse was detected.");
        }
        if (current.RefreshToken.RevokedAt.HasValue ||
            current.RefreshToken.ExpiresAt <= now ||
            current.Session.RevokedAt.HasValue ||
            current.Session.ExpiresAt <= now)
        {
            throw new UnauthorizedAccessException("Refresh token is no longer active.");
        }

        var user = await _users.GetActiveByIdAsync(current.Session.UserId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Authenticated user was not found.");
        var accessToken = _tokenService.CreateAccessToken(user.Id, user.UserName, user.DisplayName);
        var refreshToken = _tokenService.CreateRefreshToken();
        await _sessions.RotateRefreshTokenAsync(
            current,
            accessToken.TokenId,
            new RefreshToken
            {
                UserSessionId = current.Session.Id,
                TokenHash = refreshToken.Hash,
                CreatedAt = now,
                ExpiresAt = refreshToken.ExpiresAt
            },
            now,
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResponseDto
        {
            AccessToken = accessToken.Value,
            RefreshToken = refreshToken.Value,
            ExpiresAt = accessToken.ExpiresAt,
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
