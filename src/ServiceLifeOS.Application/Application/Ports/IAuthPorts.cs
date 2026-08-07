namespace ServiceLifeOS.Application.Ports;

using ServiceLifeOS.Domain.Entities;

public interface ICurrentUser
{
    string UserId { get; }

    string UserName { get; }
}

public interface IUserRepository
{
    Task<AppUser?> GetActiveByUserNameAsync(
        string userName,
        CancellationToken cancellationToken = default);

    Task<AppUser?> GetActiveByIdAsync(
        string userId,
        CancellationToken cancellationToken = default);
}

public interface IPasswordHasher
{
    string HashPassword(string password);

    bool VerifyPassword(string password, string passwordHash);
}

public interface ITokenService
{
    string CreateAccessToken(string userId, string userName, string displayName);
}
