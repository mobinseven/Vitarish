using System.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Caching.Distributed;
using Npgsql;
using Vitarish.Users.Application.Abstractions;
using Vitarish.Users.Application.Authorization;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Data;
using Vitarish.Users.Infrastructure.Extensions;

namespace Vitarish.Users.Infrastructure.Service;

public sealed class UserAccountDeletionStore(UsersDbContext context, IDistributedCache cache) : IUserAccountDeletionStore
{
    public async Task<IUserAccountDeletionScope> BeginAsync(string userId, CancellationToken cancellationToken)
    {
        UsersDbContext db = context.CreateAccountTransactionContext();
        IAsyncDisposable? gate = null;
        IDbContextTransaction? transaction = null;
        try
        {
            gate = await UserAccountWriteLock.AcquireInMemoryAsync(db, cancellationToken);
            if (db.Database.IsRelational())
                transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            User? user = await UserAccountWriteLock.ReadForUpdateAsync(db, userId, cancellationToken);
            UserAccountDeletionSnapshot? snapshot = null;
            if (user is not null)
            {
                string[] roles = await (from membership in db.UserRoles join role in db.Roles on membership.RoleId equals role.Id
                    where membership.UserId == userId select role.Name!).ToArrayAsync(cancellationToken);
                int otherAdministrators = await (from membership in db.UserRoles join role in db.Roles on membership.RoleId equals role.Id
                    join account in db.Users on membership.UserId equals account.Id
                    where account.Id != userId && account.IsEnabled && role.Name == IdentityDefinitions.PlatformAdmin
                    select account.Id).Distinct().CountAsync(cancellationToken);
                bool lastOwner = false;
                foreach (string ownerRole in roles.Where(role => TenantRoleDefinitions.IsTenantRoleName(role) &&
                    role.EndsWith(TenantRoleDefinitions.RoleSeparator + IdentityDefinitions.TenantOwner, StringComparison.Ordinal)))
                {
                    bool hasOtherOwner = await (from membership in db.UserRoles join role in db.Roles on membership.RoleId equals role.Id
                        join account in db.Users on membership.UserId equals account.Id
                        where account.Id != userId && account.IsEnabled && role.Name == ownerRole select account.Id).AnyAsync(cancellationToken);
                    if (!hasOtherOwner) { lastOwner = true; break; }
                }
                snapshot = new(user.Id, user.ConcurrencyStamp, user.IsEnabled, user.IsDeleted,
                    roles.Contains(IdentityDefinitions.PlatformAdmin, StringComparer.Ordinal), otherAdministrators, lastOwner);
            }
            return new Scope(db, cache, user, snapshot, transaction, gate);
        }
        catch (Exception exception)
        {
            if (transaction is not null) await transaction.DisposeAsync();
            if (gate is not null) await gate.DisposeAsync();
            await db.DisposeAsync();
            if (IsConcurrency(exception)) throw new UserAccountDeletionConcurrencyException(exception);
            throw;
        }
    }

    private static bool IsConcurrency(Exception exception) => exception is DbUpdateConcurrencyException ||
        exception is PostgresException { SqlState: PostgresErrorCodes.SerializationFailure or PostgresErrorCodes.DeadlockDetected } ||
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.SerializationFailure or PostgresErrorCodes.DeadlockDetected };

    private sealed class Scope(UsersDbContext db, IDistributedCache cache, User? user, UserAccountDeletionSnapshot? account,
        IDbContextTransaction? transaction, IAsyncDisposable? gate) : IUserAccountDeletionScope
    {
        public UserAccountDeletionSnapshot? Account => account;

        public async Task EraseAsync(DateTime utcNow, string securityStamp, string revision, CancellationToken cancellationToken)
        {
            if (user is null || user.IsDeleted) throw new InvalidOperationException("A live account is required for erasure.");
            try
            {
                // Evict while the old normalized name is still available. Failure aborts before erasure.
                await UserCacheExtensions.RemoveUserAsync(cache, user, cancellationToken);
                db.Attach(user);
                db.UserRoles.RemoveRange(await db.UserRoles.Where(row => row.UserId == user.Id).ToArrayAsync(cancellationToken));
                db.UserClaims.RemoveRange(await db.UserClaims.Where(row => row.UserId == user.Id).ToArrayAsync(cancellationToken));
                db.UserLogins.RemoveRange(await db.UserLogins.Where(row => row.UserId == user.Id).ToArrayAsync(cancellationToken));
                db.UserTokens.RemoveRange(await db.UserTokens.Where(row => row.UserId == user.Id).ToArrayAsync(cancellationToken));
                if (db.Model.FindEntityType(typeof(IdentityUserPasskey<string>)) is not null)
                    db.UserPasskeys.RemoveRange(await db.UserPasskeys.Where(row => row.UserId == user.Id).ToArrayAsync(cancellationToken));
                db.WebAuthnCredentials.RemoveRange(await db.WebAuthnCredentials.Where(row => row.UserId == user.Id).ToArrayAsync(cancellationToken));
                db.TemporaryAutoAuthLinks.RemoveRange(await db.TemporaryAutoAuthLinks.Where(row => row.UserId == user.Id).ToArrayAsync(cancellationToken));
                user.EraseAccount(utcNow, securityStamp, revision);
                db.DeletionInProgress = true;
                await db.SaveChangesAsync(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                // Once committing, finish independently of an HTTP disconnect. The caller
                // can safely repeat the command if the response is lost.
                if (transaction is not null) await transaction.CommitAsync(CancellationToken.None);
            }
            catch (Exception exception) when (IsConcurrency(exception))
            {
                throw new UserAccountDeletionConcurrencyException(exception);
            }
        }

        public async ValueTask DisposeAsync()
        {
            db.DeletionInProgress = false;
            try { if (transaction is not null) await transaction.DisposeAsync(); }
            finally
            {
                db.ChangeTracker.Clear();
                if (gate is not null) await gate.DisposeAsync();
                await db.DisposeAsync();
            }
        }
    }
}
