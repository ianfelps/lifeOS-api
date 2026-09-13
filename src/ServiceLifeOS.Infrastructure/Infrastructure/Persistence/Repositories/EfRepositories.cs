using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using ServiceLifeOS.Application.Ports;
using ServiceLifeOS.Domain.Entities;

namespace ServiceLifeOS.Infrastructure.Persistence.Repositories;

public sealed class EfAppTransaction : IAppTransaction
{
    private readonly IDbContextTransaction _transaction;

    public EfAppTransaction(IDbContextTransaction transaction)
    {
        _transaction = transaction;
    }

    public Task CommitAsync(CancellationToken cancellationToken = default)
    {
        return _transaction.CommitAsync(cancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        return _transaction.DisposeAsync();
    }
}

public sealed class UserRepository : IUserRepository
{
    private readonly AppDbContext _db;

    public UserRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<AppUser?> GetActiveByUserNameAsync(
        string userName,
        CancellationToken cancellationToken = default)
    {
        return _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.UserName == userName && x.Active,
                cancellationToken);
    }

    public Task<AppUser?> GetActiveByIdAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Id == userId && x.Active,
             cancellationToken);
    }

    public Task<AppUser?> GetByUserNameAsync(
        string userName,
        CancellationToken cancellationToken = default)
    {
        return _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserName == userName, cancellationToken);
    }

    public async Task UpdatePasswordHashAsync(
        string userId,
        string passwordHash,
        DateTime updatedAt,
        CancellationToken cancellationToken = default)
    {
        await _db.Users
            .Where(x => x.Id == userId && x.Active)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.PasswordHash, passwordHash)
                    .SetProperty(x => x.UpdatedAt, updatedAt),
                cancellationToken);
    }

    public async Task UpdateIdentityAsync(
        string userId,
        string userName,
        string displayName,
        DateTime updatedAt,
        CancellationToken cancellationToken = default)
    {
        await _db.Users
            .Where(x => x.Id == userId && x.Active)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.UserName, userName)
                    .SetProperty(x => x.DisplayName, displayName)
                    .SetProperty(x => x.UpdatedAt, updatedAt),
                cancellationToken);
    }
}
