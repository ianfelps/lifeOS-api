using ServiceLifeOS.Application.Ports;
using ServiceLifeOS.Application.Services;
using ServiceLifeOS.Domain.Entities;
using ServiceLifeOS.Dtos.Auth;
using Xunit;

namespace ServiceLifeOS.Tests.Application;

public sealed class AuthServiceTests
{
    [Fact]
    public async Task Login_CreatesSessionWithHashedRefreshToken()
    {
        var sessions = new FakeUserSessionRepository();
        var tokenService = new FakeTokenService();
        var service = CreateService(sessions, tokenService);

        var response = await service.LoginAsync(new()
        {
            UserName = "user",
            Password = "password"
        });

        Assert.Equal("access-1", response.AccessToken);
        Assert.Equal("refresh-1", response.RefreshToken);
        Assert.Equal(tokenService.AccessExpiration, response.ExpiresAt);
        Assert.NotNull(sessions.CreatedSession);
        Assert.NotNull(sessions.CreatedRefreshToken);
        Assert.Equal(tokenService.RefreshExpiration, sessions.CreatedSession.ExpiresAt);
        Assert.Equal("hash:refresh-1", sessions.CreatedRefreshToken.TokenHash);
    }

    [Fact]
    public async Task Refresh_RotatesTokensAndMarksPreviousRefreshTokenAsUsed()
    {
        var now = DateTime.UtcNow;
        var session = new UserSession
        {
            UserId = "user-1",
            TokenId = "access-1",
            ExpiresAt = now.AddDays(1)
        };
        var refreshToken = new RefreshToken
        {
            UserSessionId = session.Id,
            TokenHash = "hash:current-refresh",
            ExpiresAt = now.AddDays(1)
        };
        var sessions = new FakeUserSessionRepository
        {
            RefreshTokenSession = new() { Session = session, RefreshToken = refreshToken }
        };
        var tokenService = new FakeTokenService();
        var service = CreateService(sessions, tokenService);

        var response = await service.RefreshAsync(new() { RefreshToken = "current-refresh" });

        Assert.Equal("access-1", response.AccessToken);
        Assert.Equal("refresh-1", response.RefreshToken);
        Assert.NotNull(refreshToken.UsedAt);
        Assert.NotNull(refreshToken.ReplacedByRefreshTokenId);
        Assert.Equal("token-1", session.TokenId);
        Assert.NotNull(sessions.RotatedRefreshToken);
        Assert.Equal("hash:refresh-1", sessions.RotatedRefreshToken.TokenHash);
    }

    [Fact]
    public async Task Refresh_RevokesSessionWhenPreviousTokenIsReused()
    {
        var session = new UserSession { UserId = "user-1", ExpiresAt = DateTime.UtcNow.AddDays(1) };
        var sessions = new FakeUserSessionRepository
        {
            RefreshTokenSession = new()
            {
                Session = session,
                RefreshToken = new()
                {
                    UserSessionId = session.Id,
                    TokenHash = "hash:reused-refresh",
                    UsedAt = DateTime.UtcNow.AddMinutes(-1),
                    ExpiresAt = DateTime.UtcNow.AddDays(1)
                }
            }
        };
        var service = CreateService(sessions, new FakeTokenService());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.RefreshAsync(new() { RefreshToken = "reused-refresh" }));

        Assert.Equal(session.Id, sessions.RevokedSessionId);
    }

    private static AuthService CreateService(FakeUserSessionRepository sessions, FakeTokenService tokenService)
    {
        return new AuthService(
            new FakeUserRepository(),
            new FakePasswordHasher(),
            tokenService,
            sessions,
            new FakeAuditLogRepository(),
            new FakeUnitOfWork());
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        private readonly AppUser _user = new()
        {
            Id = "user-1",
            UserName = "user",
            DisplayName = "User",
            PasswordHash = "password",
            Active = true
        };

        public Task<AppUser?> GetActiveByUserNameAsync(string userName, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<AppUser?>(_user);
        }

        public Task<AppUser?> GetActiveByIdAsync(string userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<AppUser?>(_user);
        }

        public Task UpdatePasswordHashAsync(
            string userId,
            string passwordHash,
            DateTime updatedAt,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public string HashPassword(string password) => password;

        public bool VerifyPassword(string password, string passwordHash) => password == passwordHash;
    }

    private sealed class FakeTokenService : ITokenService
    {
        private int _counter;

        public DateTime AccessExpiration { get; } = DateTime.UtcNow.AddMinutes(15);

        public DateTime RefreshExpiration { get; } = DateTime.UtcNow.AddDays(30);

        public AccessTokenData CreateAccessToken(string userId, string userName, string displayName)
        {
            _counter++;
            return new()
            {
                Value = $"access-{_counter}",
                TokenId = $"token-{_counter}",
                ExpiresAt = AccessExpiration
            };
        }

        public RefreshTokenData CreateRefreshToken()
        {
            return new()
            {
                Value = "refresh-1",
                Hash = "hash:refresh-1",
                ExpiresAt = RefreshExpiration
            };
        }

        public string HashRefreshToken(string refreshToken) => $"hash:{refreshToken}";
    }

    private sealed class FakeUserSessionRepository : IUserSessionRepository
    {
        public UserSession? CreatedSession { get; private set; }

        public RefreshToken? CreatedRefreshToken { get; private set; }

        public RefreshTokenSession? RefreshTokenSession { get; init; }

        public RefreshToken? RotatedRefreshToken { get; private set; }

        public Guid? RevokedSessionId { get; private set; }

        public Task CreateAsync(
            UserSession session,
            RefreshToken refreshToken,
            CancellationToken cancellationToken = default)
        {
            CreatedSession = session;
            CreatedRefreshToken = refreshToken;
            return Task.CompletedTask;
        }

        public Task<bool> IsActiveAsync(
            string userId,
            string tokenId,
            DateTime now,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }

        public Task TouchAsync(string tokenId, DateTime now, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<int> RevokeOtherActiveSessionsAsync(
            string userId,
            string currentTokenId,
            DateTime now,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(0);
        }

        public Task<RefreshTokenSession?> GetRefreshTokenSessionAsync(
            string refreshTokenHash,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(RefreshTokenSession);
        }

        public Task RotateRefreshTokenAsync(
            RefreshTokenSession current,
            string accessTokenId,
            RefreshToken replacement,
            DateTime now,
            CancellationToken cancellationToken = default)
        {
            current.RefreshToken.UsedAt = now;
            current.RefreshToken.ReplacedByRefreshTokenId = replacement.Id;
            current.Session.TokenId = accessTokenId;
            current.Session.ExpiresAt = replacement.ExpiresAt;
            RotatedRefreshToken = replacement;
            return Task.CompletedTask;
        }

        public Task RevokeSessionAsync(
            Guid sessionId,
            DateTime now,
            CancellationToken cancellationToken = default)
        {
            RevokedSessionId = sessionId;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeAuditLogRepository : IAuditLogRepository
    {
        public Task CreateAsync(AuditLog auditLog, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<AuditLogPage> GetPageAsync(
            string userId,
            AuditLogFilter filter,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new AuditLogPage());
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
