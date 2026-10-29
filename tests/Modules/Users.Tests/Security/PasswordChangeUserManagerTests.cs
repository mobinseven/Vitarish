using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Vitarish.Users.Application.Queries;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Data;
using Vitarish.Users.Infrastructure.Handlers;
using Vitarish.Users.Infrastructure.Security;
using Vitarish.Users.Infrastructure.Service;
using Vitarish.Users.Module.DependencyInjection;

using UsersIdentityStore = Microsoft.AspNetCore.Identity.EntityFrameworkCore.UserStore<
    Vitarish.Users.Domain.Entities.User,
    Microsoft.AspNetCore.Identity.IdentityRole,
    Vitarish.Users.Infrastructure.Data.UsersDbContext,
    string,
    Microsoft.AspNetCore.Identity.IdentityUserClaim<string>,
    Microsoft.AspNetCore.Identity.IdentityUserRole<string>,
    Microsoft.AspNetCore.Identity.IdentityUserLogin<string>,
    Microsoft.AspNetCore.Identity.IdentityUserToken<string>,
    Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>,
    Microsoft.AspNetCore.Identity.IdentityUserPasskey<string>>;

namespace Vitarish.Users.Tests.Security;

public sealed class PasswordChangeUserManagerTests
{
    [Fact]
    public async Task Current_identity_writer_persists_a_matching_fingerprint_with_the_password_date()
    {
        await using UsersDbContext db = CreateDb();
        TestClock clock = new();
        using PasswordChangeUserManager manager = CreateManager(db, clock);
        User user = new() { UserName = "fingerprint-create-test" };

        (await manager.CreateAsync(user, "ValidPass1!")).Succeeded.Should().BeTrue();

        user.PasswordHash.Should().NotBeNull();
        user.PasswordHashFingerprint.Should().Equal(StoredPasswordHashFingerprint.Create(user.PasswordHash!));
        User persisted = await db.Users.AsNoTracking().SingleAsync(value => value.Id == user.Id);
        persisted.PasswordHashFingerprint.Should().Equal(user.PasswordHashFingerprint);
        (await ReadStatusAsync(db, user.Id)).Should().BeEquivalentTo(new UserPasswordStatus(true, clock.GetUtcNow().UtcDateTime));
    }

    [Fact]
    public async Task An_unchanged_hash_keeps_its_verified_password_date_after_a_profile_update()
    {
        await using UsersDbContext db = CreateDb();
        TestClock clock = new();
        using PasswordChangeUserManager manager = CreateManager(db, clock);
        User user = new() { UserName = "unchanged-hash-test" };
        (await manager.CreateAsync(user, "ValidPass1!")).Succeeded.Should().BeTrue();
        string originalHash = user.PasswordHash!;
        DateTime originalDate = user.PasswordChangedAt!.Value;
        byte[] originalFingerprint = user.PasswordHashFingerprint!.ToArray();

        user.FirstName = "Profile update";
        (await manager.UpdateAsync(user)).Succeeded.Should().BeTrue();

        user.PasswordHash.Should().Be(originalHash);
        user.PasswordHashFingerprint.Should().Equal(originalFingerprint);
        (await ReadStatusAsync(db, user.Id)).Should().BeEquivalentTo(new UserPasswordStatus(true, originalDate));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task An_older_identity_writer_makes_the_date_unknown_when_its_clock_is_same_or_earlier(int clockOffsetDays)
    {
        await using UsersDbContext db = CreateDb();
        TestClock clock = new();
        using PasswordChangeUserManager currentWriter = CreateManager(db, clock);
        User user = new() { UserName = "older-writer-test" };
        (await currentWriter.CreateAsync(user, "ValidPass1!")).Succeeded.Should().BeTrue();
        DateTime originalDate = user.PasswordChangedAt!.Value;
        string originalHash = user.PasswordHash!;
        byte[] originalFingerprint = user.PasswordHashFingerprint!.ToArray();
        clock.UtcNow = new DateTimeOffset(originalDate.AddDays(clockOffsetDays));
        using UserManager<User> olderWriter = CreatePlainManager(db);

        (await olderWriter.ChangePasswordAsync(user, "ValidPass1!", "NextValid1!")).Succeeded.Should().BeTrue();

        user.PasswordHash.Should().NotBe(originalHash);
        user.PasswordChangedAt.Should().Be(originalDate);
        user.PasswordHashFingerprint.Should().Equal(originalFingerprint);
        (await ReadStatusAsync(db, user.Id)).Should().BeEquivalentTo(new UserPasswordStatus(true, null));
    }

    [Fact]
    public async Task Assignment_and_change_persist_the_date_but_rejected_changes_do_not()
    {
        await using UsersDbContext db = CreateDb();
        TestClock clock = new();
        using PasswordChangeUserManager manager = CreateManager(db, clock);
        User user = new() { UserName = "assignment-test" };
        (await manager.CreateAsync(user)).Succeeded.Should().BeTrue();
        (await manager.AddPasswordAsync(user, "ValidPass1!")).Succeeded.Should().BeTrue();
        DateTime originalDate = clock.GetUtcNow().UtcDateTime;
        (await ReadDateAsync(db, user.Id)).Should().Be(originalDate);
        (await ReadStatusAsync(db, user.Id)).Should().BeEquivalentTo(new UserPasswordStatus(true, originalDate));

        clock.UtcNow = clock.UtcNow.AddDays(1);
        (await manager.ChangePasswordAsync(user, "incorrect", "NextValid1!")).Succeeded.Should().BeFalse();
        (await ReadDateAsync(db, user.Id)).Should().Be(originalDate);
        (await ReadStatusAsync(db, user.Id)).Should().BeEquivalentTo(new UserPasswordStatus(true, originalDate));
        (await manager.ChangePasswordAsync(user, "ValidPass1!", "NextValid1!")).Succeeded.Should().BeTrue();
        DateTime changedDate = clock.GetUtcNow().UtcDateTime;
        (await ReadDateAsync(db, user.Id)).Should().Be(changedDate);
        (await ReadStatusAsync(db, user.Id)).Should().BeEquivalentTo(new UserPasswordStatus(true, changedDate));
    }

    [Fact]
    public async Task Creating_with_a_password_records_the_date_and_legacy_rows_start_unknown()
    {
        await using UsersDbContext db = CreateDb();
        TestClock clock = new();
        using PasswordChangeUserManager manager = CreateManager(db, clock);
        User legacy = new() { UserName = "legacy-test" };
        (await manager.CreateAsync(legacy)).Succeeded.Should().BeTrue();
        (await ReadDateAsync(db, legacy.Id)).Should().BeNull();
        (await ReadStatusAsync(db, legacy.Id)).Should().BeEquivalentTo(new UserPasswordStatus(false, null));
        User created = new() { UserName = "creation-test" };
        (await manager.CreateAsync(created, "ValidPass1!")).Succeeded.Should().BeTrue();
        (await ReadDateAsync(db, created.Id)).Should().Be(clock.GetUtcNow().UtcDateTime);
        (await ReadStatusAsync(db, created.Id)).Should().BeEquivalentTo(new UserPasswordStatus(true, clock.GetUtcNow().UtcDateTime));
    }

    [Fact]
    public async Task Transparent_password_rehash_does_not_change_the_assignment_date()
    {
        await using UsersDbContext db = CreateDb();
        TestClock clock = new();
        using PasswordChangeUserManager manager = CreateManager(db, clock);
        User user = new() { UserName = "rehash-test" };
        (await manager.CreateAsync(user, "ValidPass1!")).Succeeded.Should().BeTrue();
        DateTime originalDate = clock.GetUtcNow().UtcDateTime;
        PasswordHasher<User> legacyHasher = new(Options.Create(new PasswordHasherOptions { IterationCount = 10000 }));
        user.PasswordHash = legacyHasher.HashPassword(user, "ValidPass1!");
        user.PasswordHashFingerprint = StoredPasswordHashFingerprint.Create(user.PasswordHash!);
        await db.SaveChangesAsync();
        byte[] originalFingerprint = user.PasswordHashFingerprint!.ToArray();
        string previousHash = user.PasswordHash!;
        clock.UtcNow = clock.UtcNow.AddDays(1);
        (await manager.CheckPasswordAsync(user, "ValidPass1!")).Should().BeTrue();
        user.PasswordHash.Should().NotBe(previousHash);
        (await ReadDateAsync(db, user.Id)).Should().Be(originalDate);
        user.PasswordHashFingerprint.Should().Equal(originalFingerprint);
        (await ReadStatusAsync(db, user.Id)).Should().BeEquivalentTo(new UserPasswordStatus(true, null));
    }

    [Fact]
    public async Task Removing_a_password_does_not_leave_a_known_date_or_refresh_its_fingerprint()
    {
        await using UsersDbContext db = CreateDb();
        TestClock clock = new();
        using PasswordChangeUserManager manager = CreateManager(db, clock);
        User user = new() { UserName = "password-removal-test" };
        (await manager.CreateAsync(user, "ValidPass1!")).Succeeded.Should().BeTrue();
        DateTime originalDate = user.PasswordChangedAt!.Value;
        byte[] originalFingerprint = user.PasswordHashFingerprint!.ToArray();

        (await manager.RemovePasswordAsync(user)).Succeeded.Should().BeTrue();

        user.PasswordHash.Should().BeNull();
        user.PasswordChangedAt.Should().Be(originalDate);
        user.PasswordHashFingerprint.Should().Equal(originalFingerprint);
        (await ReadStatusAsync(db, user.Id)).Should().BeEquivalentTo(new UserPasswordStatus(false, null));
    }

    [Fact]
    public async Task Reset_records_only_a_successful_assignment()
    {
        await using UsersDbContext db = CreateDb();
        TestClock clock = new();
        using PasswordChangeUserManager manager = CreateManager(db, clock);
        manager.RegisterTokenProvider(TokenOptions.DefaultProvider, new TestTokenProvider());
        User user = new() { UserName = "reset-test" };
        (await manager.CreateAsync(user, "ValidPass1!")).Succeeded.Should().BeTrue();
        DateTime originalDate = clock.GetUtcNow().UtcDateTime;
        clock.UtcNow = clock.UtcNow.AddDays(1);
        (await manager.ResetPasswordAsync(user, "invalid", "NextValid1!")).Succeeded.Should().BeFalse();
        (await ReadDateAsync(db, user.Id)).Should().Be(originalDate);
        string token = await manager.GeneratePasswordResetTokenAsync(user);
        (await manager.ResetPasswordAsync(user, token, "NextValid1!")).Succeeded.Should().BeTrue();
        (await ReadDateAsync(db, user.Id)).Should().Be(clock.GetUtcNow().UtcDateTime);
        (await ReadStatusAsync(db, user.Id)).Should().BeEquivalentTo(new UserPasswordStatus(true, clock.GetUtcNow().UtcDateTime));
    }

    [Fact]
    public async Task Rejected_store_update_restores_tracked_password_and_timestamp_values()
    {
        await using UsersDbContext db = CreateDb();
        TestClock clock = new();
        User user = new() { UserName = "store-rejection-test" };
        using (PasswordChangeUserManager manager = CreateManager(db, clock))
            (await manager.CreateAsync(user)).Succeeded.Should().BeTrue();
        DateTime? originalDate = user.PasswordChangedAt;
        string? originalHash = user.PasswordHash;

        using PasswordChangeUserManager rejectingManager = CreateManager(db, clock, new RejectingUpdateUserStore(db));
        IdentityResult result = await rejectingManager.AddPasswordAsync(user, "ValidPass1!");

        result.Succeeded.Should().BeFalse();
        user.PasswordChangedAt.Should().Be(originalDate);
        user.PasswordHash.Should().Be(originalHash);
        user.PasswordHashFingerprint.Should().BeNull();
        TrackedState(db, user).Should().Be(EntityState.Unchanged);
        await db.SaveChangesAsync();
        (await ReadDateAsync(db, user.Id)).Should().BeNull();
        (await db.Users.AsNoTracking().Where(value => value.Id == user.Id).Select(value => value.PasswordHash).SingleAsync()).Should().BeNull();
    }

    [Fact]
    public async Task Thrown_store_update_restores_tracked_values_and_propagates_the_database_failure()
    {
        await using UsersDbContext db = CreateDb();
        TestClock clock = new();
        User user = new() { UserName = "store-exception-test" };
        using (PasswordChangeUserManager manager = CreateManager(db, clock))
            (await manager.CreateAsync(user, "ValidPass1!")).Succeeded.Should().BeTrue();

        string? originalHash = user.PasswordHash;
        DateTime? originalDate = user.PasswordChangedAt;
        byte[]? originalFingerprint = user.PasswordHashFingerprint?.ToArray();
        string? originalSecurityStamp = user.SecurityStamp;
        string? originalConcurrencyStamp = user.ConcurrencyStamp;
        DbUpdateException failure = new("Simulated user-store database failure.");
        using PasswordChangeUserManager failingManager = CreateManager(db, clock, new ThrowingUpdateUserStore(db, failure));
        clock.UtcNow = clock.UtcNow.AddDays(1);

        Func<Task> assignPassword = () => failingManager.ChangePasswordAsync(user, "ValidPass1!", "NextValid1!");

        (await assignPassword.Should().ThrowAsync<DbUpdateException>()).Which.Should().BeSameAs(failure);
        user.PasswordHash.Should().Be(originalHash);
        user.PasswordChangedAt.Should().Be(originalDate);
        user.PasswordHashFingerprint.Should().Equal(originalFingerprint);
        user.SecurityStamp.Should().Be(originalSecurityStamp);
        user.ConcurrencyStamp.Should().Be(originalConcurrencyStamp);
        TrackedState(db, user).Should().Be(EntityState.Unchanged);

        await db.SaveChangesAsync();
        User persisted = await db.Users.AsNoTracking().SingleAsync(value => value.Id == user.Id);
        persisted.PasswordHash.Should().Be(originalHash);
        persisted.PasswordChangedAt.Should().Be(originalDate);
        persisted.PasswordHashFingerprint.Should().Equal(originalFingerprint);
        persisted.SecurityStamp.Should().Be(originalSecurityStamp);
        persisted.ConcurrencyStamp.Should().Be(originalConcurrencyStamp);
        (await ReadStatusAsync(db, user.Id)).Should().BeEquivalentTo(new UserPasswordStatus(true, originalDate));
    }

    [Fact]
    public async Task Overlapping_password_assignment_scopes_are_rejected_before_mutating_the_second_user()
    {
        await using UsersDbContext db = CreateDb();
        TestClock clock = new();
        using (PasswordChangeUserManager setupManager = CreateManager(db, clock))
        {
            (await setupManager.CreateAsync(new User { UserName = "concurrent-first-test" })).Succeeded.Should().BeTrue();
            (await setupManager.CreateAsync(new User { UserName = "concurrent-second-test" })).Succeeded.Should().BeTrue();
        }
        User first = await db.Users.SingleAsync(value => value.UserName == "concurrent-first-test");
        User second = await db.Users.SingleAsync(value => value.UserName == "concurrent-second-test");
        BlockingUpdateUserStore blockingStore = new(db);
        using PasswordChangeUserManager manager = CreateManager(db, clock, blockingStore);

        Task<IdentityResult> firstAssignment = manager.AddPasswordAsync(first, "ValidPass1!");
        await blockingStore.UpdateEntered.Task;
        Func<Task<IdentityResult>> overlappingAssignment = () => manager.AddPasswordAsync(second, "NextValid1!");

        await overlappingAssignment.Should().ThrowAsync<InvalidOperationException>();
        second.PasswordHash.Should().BeNull();
        second.PasswordChangedAt.Should().BeNull();
        second.PasswordHashFingerprint.Should().BeNull();
        blockingStore.AllowUpdate.TrySetResult(true);
        (await firstAssignment).Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task An_unrelated_update_in_another_execution_context_does_not_inherit_the_assignment_scope()
    {
        await using UsersDbContext db = CreateDb();
        TestClock clock = new();
        User user = new() { UserName = "isolated-update-test" };
        using (PasswordChangeUserManager setupManager = CreateManager(db, clock))
            (await setupManager.CreateAsync(user)).Succeeded.Should().BeTrue();

        SwitchableUserValidator validator = new();
        BlockingUpdateUserStore blockingStore = new(db);
        using PasswordChangeUserManager manager = CreateManager(db, clock, blockingStore, [validator]);
        Task<IdentityResult> passwordAssignment = manager.AddPasswordAsync(user, "ValidPass1!");
        await blockingStore.UpdateEntered.Task;
        byte[] assignmentFingerprint = user.PasswordHashFingerprint!;
        validator.RejectValidation = true;

        Task<IdentityResult> unrelatedUpdate;
        using (ExecutionContext.SuppressFlow())
            unrelatedUpdate = Task.Run(() => manager.UpdateAsync(user));

        (await unrelatedUpdate).Succeeded.Should().BeFalse();
        user.PasswordHashFingerprint.Should().BeSameAs(assignmentFingerprint);
        blockingStore.UpdateCount.Should().Be(1);
        blockingStore.AllowUpdate.TrySetResult(true);
        (await passwordAssignment).Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task A_captured_child_cannot_reuse_an_assignment_scope_after_it_closes()
    {
        await using UsersDbContext db = CreateDb();
        TestClock clock = new();
        User user = new() { UserName = "closed-scope-test" };
        using (PasswordChangeUserManager setupManager = CreateManager(db, clock))
            (await setupManager.CreateAsync(user)).Succeeded.Should().BeTrue();

        CapturingUpdateValidator validator = new(user);
        using PasswordChangeUserManager manager = CreateManager(db, clock, userValidators: [validator]);
        (await manager.AddPasswordAsync(user, "ValidPass1!")).Succeeded.Should().BeTrue();
        byte[] originalFingerprint = user.PasswordHashFingerprint!.ToArray();
        DateTime originalDate = user.PasswordChangedAt!.Value;
        Task<IdentityResult> deferredUpdate = validator.DeferredUpdate
            ?? throw new InvalidOperationException("The validator did not capture its deferred update.");

        user.PasswordHash = new PasswordHasher<User>().HashPassword(user, "ValidPass1!");
        await db.SaveChangesAsync();
        validator.ContinueDeferredUpdate.TrySetResult(true);

        (await deferredUpdate).Succeeded.Should().BeTrue();
        user.PasswordChangedAt.Should().Be(originalDate);
        user.PasswordHashFingerprint.Should().Equal(originalFingerprint);
        (await ReadStatusAsync(db, user.Id)).Should().BeEquivalentTo(new UserPasswordStatus(true, null));
    }

    [Fact]
    public async Task Production_composition_rolls_back_the_actual_transient_store_context()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        string databaseName = Guid.NewGuid().ToString("N");
        builder.AddUsers(options => options.UseInMemoryDatabase(databaseName));
        builder.Services.RemoveAll<TimeProvider>();
        TestClock clock = new();
        builder.Services.AddSingleton<TimeProvider>(clock);

        await using ServiceProvider provider = builder.Services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using IServiceScope scope = provider.CreateScope();
        PasswordChangeUserManager manager = scope.ServiceProvider.GetRequiredService<PasswordChangeUserManager>();
        UsersIdentityStore store = scope.ServiceProvider.GetRequiredService<IUserStore<User>>()
            .Should().BeOfType<UsersIdentityStore>().Subject;
        store.Context.Should().BeOfType<UsersDbContext>();

        User user = new() { UserName = "production-composition-test" };
        (await manager.CreateAsync(user)).Succeeded.Should().BeTrue();
        string originalConcurrencyStamp = user.ConcurrencyStamp!;

        string competingConcurrencyStamp;
        using (IServiceScope competingScope = provider.CreateScope())
        {
            UsersDbContext competingContext = competingScope.ServiceProvider.GetRequiredService<UsersDbContext>();
            User competingUser = await competingContext.Users.SingleAsync(value => value.Id == user.Id);
            competingConcurrencyStamp = Guid.NewGuid().ToString("N");
            competingUser.ConcurrencyStamp = competingConcurrencyStamp;
            await competingContext.SaveChangesAsync();
        }

        IdentityResult result = await manager.AddPasswordAsync(user, "ValidPass1!");

        result.Succeeded.Should().BeFalse();
        result.Errors.Select(error => error.Code).Should().Contain(new IdentityErrorDescriber().ConcurrencyFailure().Code);
        user.PasswordHash.Should().BeNull();
        user.PasswordChangedAt.Should().BeNull();
        user.ConcurrencyStamp.Should().Be(originalConcurrencyStamp);
        store.Context.Entry(user).State.Should().Be(EntityState.Unchanged);
        await store.Context.SaveChangesAsync();

        using IServiceScope verificationScope = provider.CreateScope();
        UsersDbContext verificationContext = verificationScope.ServiceProvider.GetRequiredService<UsersDbContext>();
        User persisted = await verificationContext.Users.AsNoTracking().SingleAsync(value => value.Id == user.Id);
        persisted.PasswordHash.Should().BeNull();
        persisted.PasswordChangedAt.Should().BeNull();
        persisted.ConcurrencyStamp.Should().Be(competingConcurrencyStamp);
    }

    [Fact]
    public void Non_EF_user_store_is_rejected_at_composition()
    {
        using ServiceProvider services = new ServiceCollection().BuildServiceProvider();
        Action createManager = () => new PasswordChangeUserManager(
            Mock.Of<IUserStore<User>>(), Options.Create(new IdentityOptions()), new PasswordHasher<User>(),
            [new UserValidator<User>()], [new PasswordValidator<User>()], new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(), services, NullLogger<UserManager<User>>.Instance, new TestClock());

        createManager.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("Password assignment requires the configured EF Users store.");
    }

    private static UsersDbContext CreateDb() => new(new DbContextOptionsBuilder<UsersDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options);

    private static PasswordChangeUserManager CreateManager(
        UsersDbContext db,
        TimeProvider clock,
        IUserStore<User>? store = null,
        IEnumerable<IUserValidator<User>>? userValidators = null) => new(
        store ?? new UsersIdentityStore(db), Options.Create(new IdentityOptions()), new PasswordHasher<User>(),
        userValidators ?? new IUserValidator<User>[] { new UserValidator<User>() }, [new PasswordValidator<User>()], new UpperInvariantLookupNormalizer(),
        new IdentityErrorDescriber(), new ServiceCollection().BuildServiceProvider(),
        NullLogger<UserManager<User>>.Instance, clock);

    private static UserManager<User> CreatePlainManager(UsersDbContext db) => new(
        new UsersIdentityStore(db), Options.Create(new IdentityOptions()), new PasswordHasher<User>(),
        [new UserValidator<User>()], [new PasswordValidator<User>()], new UpperInvariantLookupNormalizer(),
        new IdentityErrorDescriber(), new ServiceCollection().BuildServiceProvider(),
        NullLogger<UserManager<User>>.Instance);

    private static Task<UserPasswordStatus?> ReadStatusAsync(UsersDbContext db, string userId) =>
        new GetUserPasswordStatusQueryHandler(db).Handle(new GetUserPasswordStatusQuery(userId), CancellationToken.None);

    private static EntityState TrackedState(UsersDbContext db, User user) => db.Entry(user).State;

    private sealed class RejectingUpdateUserStore(UsersDbContext db) : UsersIdentityStore(db)
    {
        public bool RejectUpdates { get; set; } = true;

        public override Task<IdentityResult> UpdateAsync(User user, CancellationToken cancellationToken = default)
        {
            if (!RejectUpdates)
                return base.UpdateAsync(user, cancellationToken);

            Context.Users.Update(user);
            return Task.FromResult(IdentityResult.Failed(new IdentityErrorDescriber().ConcurrencyFailure()));
        }
    }

    private sealed class ThrowingUpdateUserStore(UsersDbContext db, DbUpdateException failure) : UsersIdentityStore(db)
    {
        public override Task<IdentityResult> UpdateAsync(User user, CancellationToken cancellationToken = default)
        {
            Context.Users.Update(user);
            throw failure;
        }
    }

    private sealed class BlockingUpdateUserStore(UsersDbContext db) : UsersIdentityStore(db)
    {
        private int updateCount;

        public TaskCompletionSource<bool> UpdateEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<bool> AllowUpdate { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int UpdateCount => Volatile.Read(ref updateCount);

        public override async Task<IdentityResult> UpdateAsync(User user, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref updateCount);
            UpdateEntered.TrySetResult(true);
            await AllowUpdate.Task.WaitAsync(cancellationToken);
            return await base.UpdateAsync(user, cancellationToken);
        }
    }

    private sealed class SwitchableUserValidator : IUserValidator<User>
    {
        private int rejectValidation;

        public bool RejectValidation
        {
            get => Volatile.Read(ref rejectValidation) != 0;
            set => Volatile.Write(ref rejectValidation, value ? 1 : 0);
        }

        public Task<IdentityResult> ValidateAsync(UserManager<User> manager, User user) =>
            Task.FromResult(RejectValidation
                ? IdentityResult.Failed(new IdentityError { Code = "RejectedForTest", Description = "Test-only validation rejection." })
                : IdentityResult.Success);
    }

    private sealed class CapturingUpdateValidator(User target) : IUserValidator<User>
    {
        private int updateCaptured;

        public TaskCompletionSource<bool> ContinueDeferredUpdate { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task<IdentityResult>? DeferredUpdate { get; private set; }

        public Task<IdentityResult> ValidateAsync(UserManager<User> manager, User user)
        {
            if (ReferenceEquals(user, target) && Interlocked.CompareExchange(ref updateCaptured, 1, 0) == 0)
            {
                DeferredUpdate = Task.Run(async () =>
                {
                    await ContinueDeferredUpdate.Task;
                    return await manager.UpdateAsync(target);
                });
            }

            return Task.FromResult(IdentityResult.Success);
        }
    }

    private static Task<DateTime?> ReadDateAsync(UsersDbContext db, string userId) =>
        db.Users.AsNoTracking().Where(user => user.Id == userId).Select(user => user.PasswordChangedAt).SingleAsync();

    private sealed class TestClock : TimeProvider
    {
        public DateTimeOffset UtcNow { get; set; } = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => UtcNow;
    }

    private sealed class TestTokenProvider : IUserTwoFactorTokenProvider<User>
    {
        private const string ValidToken = "test-token";
        public Task<string> GenerateAsync(string purpose, UserManager<User> manager, User user) => Task.FromResult(ValidToken);
        public Task<bool> ValidateAsync(string purpose, string token, UserManager<User> manager, User user) => Task.FromResult(token == ValidToken);
        public Task<bool> CanGenerateTwoFactorTokenAsync(UserManager<User> manager, User user) => Task.FromResult(false);
    }
}
