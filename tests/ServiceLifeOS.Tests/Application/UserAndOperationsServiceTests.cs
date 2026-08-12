using ServiceLifeOS.Application.Ports;
using ServiceLifeOS.Application.Services;
using ServiceLifeOS.Domain.Entities;
using ServiceLifeOS.Dtos.Operations;
using ServiceLifeOS.Dtos.Users;
using Xunit;

namespace ServiceLifeOS.Tests.Application;

public sealed class UserAndOperationsServiceTests
{
    [Fact]
    public async Task ChangePassword_RevokesOtherSessionsAndWritesAuditLogs()
    {
        var users = new FakeUserRepository(new AppUser
        {
            Id = "user-1",
            UserName = "user",
            DisplayName = "User",
            PasswordHash = "current",
            Active = true
        });
        var sessions = new FakeUserSessionRepository { RevokedSessionCount = 2 };
        var auditLogs = new FakeAuditLogRepository();
        var service = new UserService(
            users,
            new FakeUserPreferenceRepository(),
            sessions,
            auditLogs,
            new FakePasswordHasher(),
            new FakeUnitOfWork());

        await service.ChangePasswordAsync("user-1", "current-token", new()
        {
            CurrentPassword = "current",
            NewPassword = "new"
        });

        Assert.Equal("new", Assert.IsType<AppUser>(users.User).PasswordHash);
        Assert.Equal("user-1", sessions.UserId);
        Assert.Equal("current-token", sessions.CurrentTokenId);
        Assert.Contains(auditLogs.Items, x => x.Action == AuditAction.PasswordChanged);
        Assert.Contains(auditLogs.Items, x => x.Action == AuditAction.SessionsRevoked);
    }

    [Fact]
    public async Task UpdatePreference_WritesThePreviousAndCurrentValues()
    {
        var preference = new UserPreference
        {
            UserId = "user-1",
            PreferredWeightUnit = WeightUnit.Kilograms
        };
        var auditLogs = new FakeAuditLogRepository();
        var service = new UserService(
            new FakeUserRepository(null),
            new FakeUserPreferenceRepository(preference),
            new FakeUserSessionRepository(),
            auditLogs,
            new FakePasswordHasher(),
            new FakeUnitOfWork());

        var result = await service.UpdatePreferenceAsync("user-1", new()
        {
            PreferredWeightUnit = WeightUnit.Pounds
        });

        var auditLog = Assert.Single(auditLogs.Items);
        Assert.Equal(WeightUnit.Pounds, result.PreferredWeightUnit);
        Assert.Equal(AuditAction.Updated, auditLog.Action);
        Assert.Contains("\"PreferredWeightUnit\":0", auditLog.PreviousValues);
        Assert.Contains("\"PreferredWeightUnit\":1", auditLog.CurrentValues);
    }

    [Fact]
    public async Task GetAuditLogs_MapsTheFilteredPage()
    {
        var auditLogs = new FakeAuditLogRepository
        {
            Page = new AuditLogPage
            {
                TotalCount = 1,
                Items = [new AuditLog
                {
                    UserId = "user-1",
                    Action = AuditAction.Updated,
                    ResourceType = "UserPreference",
                    PreviousValues = "previous",
                    CurrentValues = "current",
                    CreatedAt = DateTime.UtcNow
                }]
            }
        };
        var service = new OperationsService(auditLogs);

        var result = await service.GetAuditLogsAsync("user-1", new()
        {
            Page = 2,
            PageSize = 10,
            Action = AuditAction.Updated,
            ResourceType = " UserPreference "
        });

        var item = Assert.Single(result.Items);
        Assert.Equal(2, result.Page);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(1, result.TotalCount);
        Assert.Equal("previous", item.PreviousValues);
        Assert.Equal("current", item.CurrentValues);
        Assert.Equal("user-1", auditLogs.UserId);
        Assert.Equal("UserPreference", auditLogs.Filter.ResourceType);
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        public FakeUserRepository(AppUser? user)
        {
            User = user;
        }

        public AppUser? User { get; }

        public Task<AppUser?> GetActiveByUserNameAsync(string userName, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(User);
        }

        public Task<AppUser?> GetActiveByIdAsync(string userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(User);
        }

        public Task UpdatePasswordHashAsync(string userId, string passwordHash, DateTime updatedAt, CancellationToken cancellationToken = default)
        {
            if (User is not null)
            {
                User.PasswordHash = passwordHash;
                User.UpdatedAt = updatedAt;
            }

            return Task.CompletedTask;
        }
    }

    private sealed class FakeUserPreferenceRepository : IUserPreferenceRepository
    {
        private readonly UserPreference? _preference;

        public FakeUserPreferenceRepository(UserPreference? preference = null)
        {
            _preference = preference;
        }

        public Task<UserPreference?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_preference);
        }
    }

    private sealed class FakeUserSessionRepository : IUserSessionRepository
    {
        public int RevokedSessionCount { get; set; }

        public string UserId { get; private set; } = string.Empty;

        public string CurrentTokenId { get; private set; } = string.Empty;

        public Task CreateAsync(UserSession session, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<bool> IsActiveAsync(string userId, string tokenId, DateTime now, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }

        public Task TouchAsync(string tokenId, DateTime now, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<int> RevokeOtherActiveSessionsAsync(string userId, string currentTokenId, DateTime now, CancellationToken cancellationToken = default)
        {
            UserId = userId;
            CurrentTokenId = currentTokenId;
            return Task.FromResult(RevokedSessionCount);
        }
    }

    private sealed class FakeAuditLogRepository : IAuditLogRepository
    {
        public List<AuditLog> Items { get; } = [];

        public AuditLogPage Page { get; set; } = new();

        public string UserId { get; private set; } = string.Empty;

        public AuditLogFilter Filter { get; private set; } = new();

        public Task CreateAsync(AuditLog auditLog, CancellationToken cancellationToken = default)
        {
            Items.Add(auditLog);
            return Task.CompletedTask;
        }

        public Task<AuditLogPage> GetPageAsync(string userId, AuditLogFilter filter, CancellationToken cancellationToken = default)
        {
            UserId = userId;
            Filter = filter;
            return Task.FromResult(Page);
        }
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public string HashPassword(string password)
        {
            return password;
        }

        public bool VerifyPassword(string password, string passwordHash)
        {
            return password == passwordHash;
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(1);
        }

        public Task<IAppTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}
