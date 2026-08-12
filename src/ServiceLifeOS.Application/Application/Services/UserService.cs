using System.Text.Json;
using ServiceLifeOS.Application.Ports;
using ServiceLifeOS.Domain.Entities;
using ServiceLifeOS.Dtos.Users;

namespace ServiceLifeOS.Application.Services;

public sealed class UserService
{
    private readonly IUserRepository _users;
    private readonly IUserPreferenceRepository _preferences;
    private readonly IUserSessionRepository _sessions;
    private readonly IAuditLogRepository _auditLogs;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;

    public UserService(
        IUserRepository users,
        IUserPreferenceRepository preferences,
        IUserSessionRepository sessions,
        IAuditLogRepository auditLogs,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork)
    {
        _users = users;
        _preferences = preferences;
        _sessions = sessions;
        _auditLogs = auditLogs;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
    }

    public async Task ChangePasswordAsync(
        string userId,
        string currentTokenId,
        ChangePasswordRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.CurrentPassword) ||
            string.IsNullOrWhiteSpace(request.NewPassword))
        {
            throw new ArgumentException("Current and new passwords are required.");
        }

        var user = await _users.GetActiveByIdAsync(userId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Authenticated user was not found.");
        if (!_passwordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Current password is invalid.");
        }

        var now = DateTime.UtcNow;
        await _users.UpdatePasswordHashAsync(
            userId,
            _passwordHasher.HashPassword(request.NewPassword),
            now,
            cancellationToken);
        var revokedSessionCount = await _sessions.RevokeOtherActiveSessionsAsync(
            userId,
            currentTokenId,
            now,
            cancellationToken);
        await _auditLogs.CreateAsync(new()
        {
            UserId = userId,
            Action = AuditAction.PasswordChanged,
            ResourceType = "User",
            CreatedAt = now
        }, cancellationToken);
        if (revokedSessionCount > 0)
        {
            await _auditLogs.CreateAsync(new()
            {
                UserId = userId,
                Action = AuditAction.SessionsRevoked,
                ResourceType = "UserSession",
                CurrentValues = JsonSerializer.Serialize(new { RevokedSessionCount = revokedSessionCount }),
                CreatedAt = now
            }, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<UserPreferenceResponseDto> GetPreferenceAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var preference = await _preferences.GetByUserIdAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("User preference was not found.");
        return new() { PreferredWeightUnit = preference.PreferredWeightUnit };
    }

    public async Task<UserPreferenceResponseDto> UpdatePreferenceAsync(
        string userId,
        UpdateUserPreferenceRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(request.PreferredWeightUnit))
        {
            throw new ArgumentException("Preferred weight unit is invalid.");
        }

        var preference = await _preferences.GetByUserIdAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("User preference was not found.");
        var previousWeightUnit = preference.PreferredWeightUnit;
        preference.PreferredWeightUnit = request.PreferredWeightUnit;
        preference.UpdatedAt = DateTime.UtcNow;
        await _auditLogs.CreateAsync(new()
        {
            UserId = userId,
            Action = AuditAction.Updated,
            ResourceType = "UserPreference",
            ResourceId = preference.Id,
            PreviousValues = JsonSerializer.Serialize(new { PreferredWeightUnit = previousWeightUnit }),
            CurrentValues = JsonSerializer.Serialize(new { PreferredWeightUnit = preference.PreferredWeightUnit }),
            CreatedAt = preference.UpdatedAt
        }, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new() { PreferredWeightUnit = preference.PreferredWeightUnit };
    }

    public async Task<RevokeOtherSessionsResponseDto> RevokeOtherSessionsAsync(
        string userId,
        string currentTokenId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var revokedSessionCount = await _sessions.RevokeOtherActiveSessionsAsync(
            userId,
            currentTokenId,
            now,
            cancellationToken);
        await _auditLogs.CreateAsync(new()
        {
            UserId = userId,
            Action = AuditAction.SessionsRevoked,
            ResourceType = "UserSession",
            CurrentValues = JsonSerializer.Serialize(new { RevokedSessionCount = revokedSessionCount }),
            CreatedAt = now
        }, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new() { RevokedSessionCount = revokedSessionCount };
    }
}
