using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Vitarish.Users.Application.Abstractions;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Data;

namespace Vitarish.Users.Infrastructure.Service;

public sealed class UserSignInGuard(UsersDbContext context) : IUserSignInGuard
{
    public async Task<IUserSignInLease> AcquireAsync(string userId, CancellationToken cancellationToken)
    {
        UsersDbContext db = context.CreateAccountTransactionContext();
        IAsyncDisposable? gate = null;
        IDbContextTransaction? transaction = null;
        try
        {
            gate = await UserAccountWriteLock.AcquireInMemoryAsync(db, cancellationToken);
            if (db.Database.IsRelational())
                transaction = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
            User? user = await UserAccountWriteLock.ReadForUpdateAsync(db, userId, cancellationToken);
            return new Lease(user is { IsDeleted: false }, db, transaction, gate);
        }
        catch
        {
            if (transaction is not null) await transaction.DisposeAsync();
            if (gate is not null) await gate.DisposeAsync();
            await db.DisposeAsync();
            throw;
        }
    }

    private sealed class Lease(bool available, UsersDbContext db, IDbContextTransaction? transaction, IAsyncDisposable? gate) : IUserSignInLease
    {
        public bool AccountAvailable => available;
        public async ValueTask DisposeAsync()
        {
            try { if (transaction is not null) await transaction.DisposeAsync(); }
            finally
            {
                if (gate is not null) await gate.DisposeAsync();
                await db.DisposeAsync();
            }
        }
    }
}
