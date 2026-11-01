using System.Data.Common;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Testcontainers.PostgreSql;
using Vitarish.Users.Application.Abstractions;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Data;
using Vitarish.Users.Infrastructure.Service;

namespace Vitarish.Users.Tests.Data;

public sealed class UserAccountDeletionPersistenceTests
{
    private const string AccountId = "persisted-deletion-user";
    private const string Contact = "persisted-deletion@example.test";
    private const string Revision = "persisted-revision";

    [DockerFact]
    public async Task Migration_preserves_existing_data_then_erasure_releases_unique_identifiers_without_removing_the_id()
    {
        await using PostgreSqlContainer container = PostgresTestSupport.CreateContainer();
        await container.StartAsync();
        string connection = await PostgresTestSupport.CreateDatabaseAsync(container);
        await using UsersDbContext current = PostgresTestSupport.CreateContext(connection);
        string[] migrations = current.Database.GetMigrations().ToArray();
        int index = Array.FindIndex(migrations, value => value.EndsWith("_" + nameof(Vitarish.Users.Infrastructure.Migrations.UsersDb.UserAccountDeletion), StringComparison.Ordinal));
        index.Should().BeGreaterThan(0);
        await current.GetService<IMigrator>().MigrateAsync(migrations[index - 1]);
        await using (UsersDbContext legacy = PostgresTestSupport.CreateContextWithoutDeletion(connection))
            await SeedAsync(legacy);
        await current.GetService<IMigrator>().MigrateAsync(migrations[index]);
        User user = await current.Users.AsNoTracking().SingleAsync();
        user.DeletedAt.Should().BeNull();
        user.Email.Should().Be(Contact);
        user.IsEnabled.Should().BeTrue();
        await using (IUserAccountDeletionScope deletion = await Store(current).BeginAsync(AccountId, default))
            await deletion.EraseAsync(DateTime.UtcNow, "rotated", "deleted-revision", default);
        (await current.Users.CountAsync()).Should().Be(0);
        (await current.Users.IgnoreQueryFilters().SingleAsync()).Id.Should().Be(AccountId);
        current.Users.Add(new User { Id = "new-contact-owner", UserName = Contact, NormalizedUserName = Contact.ToUpperInvariant(),
            Email = Contact, NormalizedEmail = Contact.ToUpperInvariant(), PhoneNumber = "+989121234567" });
        await current.SaveChangesAsync();
        (await current.Users.SingleAsync()).Id.Should().NotBe(AccountId);
        (await current.UserTokens.CountAsync()).Should().Be(0);
    }

    [DockerFact]
    public async Task Failure_after_sql_save_rolls_back_account_and_credentials_and_a_later_attempt_recovers()
    {
        await using PostgreSqlContainer container = PostgresTestSupport.CreateContainer();
        await container.StartAsync();
        string connection = await PostgresTestSupport.CreateDatabaseAsync(container);
        FailAfterSave failure = new();
        await using UsersDbContext db = new(new DbContextOptionsBuilder<UsersDbContext>()
            .UseNpgsql(connection, provider => provider.EnableRetryOnFailure()).AddInterceptors(failure).Options);
        await db.Database.MigrateAsync();
        await SeedAsync(db);
        failure.Enabled = true;
        Func<Task> erase = async () =>
        {
            await using IUserAccountDeletionScope scope = await Store(db).BeginAsync(AccountId, default);
            await scope.EraseAsync(DateTime.UtcNow, "rotated", "deleted-revision", default);
        };
        await erase.Should().ThrowAsync<IOException>();
        await using UsersDbContext verify = PostgresTestSupport.CreateContext(connection);
        (await verify.Users.SingleAsync()).Email.Should().Be(Contact);
        (await verify.UserTokens.CountAsync()).Should().Be(1);
        failure.Enabled = false;
        await erase();
        (await verify.Users.AsNoTracking().CountAsync()).Should().Be(0);
        (await verify.UserTokens.AsNoTracking().CountAsync()).Should().Be(0);
    }

    [DockerFact]
    public async Task Deletion_waits_for_a_sign_in_lease_before_checking_active_sessions()
    {
        await using PostgreSqlContainer container = PostgresTestSupport.CreateContainer();
        await container.StartAsync();
        string connection = await PostgresTestSupport.CreateDatabaseAsync(container);
        await using UsersDbContext db = PostgresTestSupport.CreateContext(connection);
        await db.Database.MigrateAsync();
        await SeedAsync(db);
        IUserSignInLease signIn = await new UserSignInGuard(db).AcquireAsync(AccountId, default);
        signIn.AccountAvailable.Should().BeTrue();
        RowLockObserver observer = new();
        await using UsersDbContext deletionContext = new(new DbContextOptionsBuilder<UsersDbContext>()
            .UseNpgsql(connection).AddInterceptors(observer).Options);
        Task<IUserAccountDeletionScope> pending = Store(deletionContext).BeginAsync(AccountId, default);
        await observer.Started.Task.WaitAsync(TimeSpan.FromSeconds(10));
        pending.IsCompleted.Should().BeFalse("the account lock must not permit erasure while session establishment is unfinished");
        await signIn.DisposeAsync();
        await using IUserAccountDeletionScope ready = await pending.WaitAsync(TimeSpan.FromSeconds(10));
        ready.Account!.Deleted.Should().BeFalse();
        // Application checks the authoritative active-session port only after Begin returns.
    }

    [DockerFact]
    public async Task A_sign_in_waiting_for_deletion_observes_the_committed_tombstone()
    {
        await using PostgreSqlContainer container = PostgresTestSupport.CreateContainer();
        await container.StartAsync();
        string connection = await PostgresTestSupport.CreateDatabaseAsync(container);
        await using UsersDbContext db = PostgresTestSupport.CreateContext(connection);
        await db.Database.MigrateAsync();
        await SeedAsync(db);
        IUserAccountDeletionScope deletion = await Store(db).BeginAsync(AccountId, default);
        RowLockObserver observer = new();
        await using UsersDbContext signInContext = new(new DbContextOptionsBuilder<UsersDbContext>()
            .UseNpgsql(connection).AddInterceptors(observer).Options);
        Task<IUserSignInLease> pending = new UserSignInGuard(signInContext).AcquireAsync(AccountId, default);
        await observer.Started.Task.WaitAsync(TimeSpan.FromSeconds(10));
        pending.IsCompleted.Should().BeFalse();
        await deletion.EraseAsync(DateTime.UtcNow, "rotated", "deleted-revision", default);
        await deletion.DisposeAsync();
        await using IUserSignInLease refused = await pending.WaitAsync(TimeSpan.FromSeconds(10));
        refused.AccountAvailable.Should().BeFalse();
    }

    private static UserAccountDeletionStore Store(UsersDbContext db) => new(db,
        new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions())));

    private static async Task SeedAsync(UsersDbContext db)
    {
        db.Users.Add(new User { Id = AccountId, UserName = Contact, NormalizedUserName = Contact.ToUpperInvariant(),
            Email = Contact, NormalizedEmail = Contact.ToUpperInvariant(), PhoneNumber = "+989121234567", ConcurrencyStamp = Revision });
        db.UserTokens.Add(new IdentityUserToken<string> { UserId = AccountId, LoginProvider = "test", Name = "test-token", Value = "protected-value" });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
    }

    private sealed class FailAfterSave : SaveChangesInterceptor
    {
        public bool Enabled { get; set; }
        public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default) =>
            Enabled ? ValueTask.FromException<int>(new IOException("Injected post-save failure.")) : ValueTask.FromResult(result);
    }

    private sealed class RowLockObserver : DbCommandInterceptor
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData,
            InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("FOR UPDATE", StringComparison.Ordinal)) Started.TrySetResult();
            return ValueTask.FromResult(result);
        }
    }
}
