using Microsoft.AspNetCore.Mvc;
using ServiceLifeOS.Api.Controllers;
using ServiceLifeOS.Application.Ports;
using ServiceLifeOS.Application.Services;
using ServiceLifeOS.Domain.Entities;
using ServiceLifeOS.Dtos.Users;
using Xunit;

namespace ServiceLifeOS.Tests.Api;

public sealed class UsersControllerTests
{
    [Fact]
    public async Task GetPreferences_ReturnsAuthenticatedUserPreference()
    {
        var controller = CreateController(new UserPreference { UserId = "user-1", PreferredWeightUnit = WeightUnit.Pounds });

        var result = await controller.GetPreferences(CancellationToken.None);

        var response = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(WeightUnit.Pounds, Assert.IsType<UserPreferenceResponseDto>(response.Value).PreferredWeightUnit);
    }

    [Fact]
    public async Task UpdatePreferences_ReturnsBadRequestForInvalidUnit()
    {
        var controller = CreateController(new UserPreference { UserId = "user-1" });

        var result = await controller.UpdatePreferences(new() { PreferredWeightUnit = (WeightUnit)99 }, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task ChangePassword_ReturnsUnauthorizedForInvalidCurrentPassword()
    {
        var controller = CreateController(new UserPreference { UserId = "user-1" });

        var result = await controller.ChangePassword(new() { CurrentPassword = "invalid", NewPassword = "new-password" }, CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task RevokeOtherSessions_ReturnsRevokedCount()
    {
        var sessions = new FakeUserSessionRepository { RevokedSessionCount = 2 };
        var controller = CreateController(new UserPreference { UserId = "user-1" }, sessions);

        var result = await controller.RevokeOtherSessions(CancellationToken.None);

        var response = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(2, Assert.IsType<RevokeOtherSessionsResponseDto>(response.Value).RevokedSessionCount);
    }

    private static UsersController CreateController(UserPreference preference, FakeUserSessionRepository? sessions = null)
    {
        var users = new FakeUserRepository(new AppUser { Id = "user-1", UserName = "user", DisplayName = "User", PasswordHash = "current", Active = true });
        var service = new UserService(users, new FakeUserPreferenceRepository(preference), sessions ?? new(), new FakeAuditLogRepository(), new FakePasswordHasher(), new FakeUnitOfWork());
        return new UsersController(service, new FakeCurrentUser());
    }

    private sealed class FakeCurrentUser : ICurrentUser
    {
        public string UserId => "user-1";
        public string UserName => "user";
        public string TokenId => "current-token";
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        private readonly AppUser _user;
        public FakeUserRepository(AppUser user) => _user = user;
        public Task<AppUser?> GetActiveByUserNameAsync(string userName, CancellationToken cancellationToken = default) => Task.FromResult<AppUser?>(_user);
        public Task<AppUser?> GetActiveByIdAsync(string userId, CancellationToken cancellationToken = default) => Task.FromResult<AppUser?>(_user);
        public Task UpdatePasswordHashAsync(string userId, string passwordHash, DateTime updatedAt, CancellationToken cancellationToken = default) { _user.PasswordHash = passwordHash; return Task.CompletedTask; }
    }

    private sealed class FakeUserPreferenceRepository : IUserPreferenceRepository
    {
        private readonly UserPreference _preference;
        public FakeUserPreferenceRepository(UserPreference preference) => _preference = preference;
        public Task<UserPreference?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default) => Task.FromResult<UserPreference?>(_preference);
    }

    private sealed class FakeUserSessionRepository : IUserSessionRepository
    {
        public int RevokedSessionCount { get; set; }
        public Task CreateAsync(UserSession session, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<bool> IsActiveAsync(string userId, string tokenId, DateTime now, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task TouchAsync(string tokenId, DateTime now, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<int> RevokeOtherActiveSessionsAsync(string userId, string currentTokenId, DateTime now, CancellationToken cancellationToken = default) => Task.FromResult(RevokedSessionCount);
    }

    private sealed class FakeAuditLogRepository : IAuditLogRepository
    {
        public Task CreateAsync(AuditLog auditLog, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<AuditLogPage> GetPageAsync(string userId, AuditLogFilter filter, CancellationToken cancellationToken = default) => Task.FromResult(new AuditLogPage());
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public string HashPassword(string password) => password;
        public bool VerifyPassword(string password, string passwordHash) => password == passwordHash;
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
        public Task<IAppTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
