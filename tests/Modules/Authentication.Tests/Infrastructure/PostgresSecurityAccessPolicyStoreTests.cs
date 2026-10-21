using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Domain.Methods;
using Vitarish.Authentication.Domain.Security;
using Vitarish.Authentication.Infrastructure;
using Xunit;

using Authentication.Tests.Infrastructure;

namespace Vitarish.Authentication.Tests.Infrastructure;

[Collection(AuthenticationPostgresCollection.Name)]
public sealed class PostgresSecurityAccessPolicyStoreTests
{
    [PostgresFact]
    public async Task First_save_moves_the_missing_policy_from_version_zero_to_one()
    {
        string connectionString = ConnectionString();

        DbContextOptions<AuthenticationDbContext> options = await PrepareAsync(connectionString);
        await using AuthenticationDbContext context = new(options);
        SecurityAccessPolicyStore store = new(context);
        (await store.GetStateAsync(CancellationToken.None)).Version.Should().Be(0);

        SecurityAccessPolicyState result = await store.SaveAsync(SecurityAccessPolicy.Default, 0, CancellationToken.None);

        result.Version.Should().Be(1);
        (await context.SecurityAccessPolicies.AsNoTracking().SingleAsync()).Version.Should().Be(1);
    }

    [PostgresFact]
    public async Task A_second_save_based_on_the_missing_policy_is_refused()
    {
        string connectionString = ConnectionString();

        DbContextOptions<AuthenticationDbContext> options = await PrepareAsync(connectionString);
        _ = await SaveAsync(options, SecurityAccessPolicy.Default, 0);

        Func<Task> staleFirstSave = () => SaveAsync(options, SecurityAccessPolicy.ProductEvaluated, 0);

        await staleFirstSave.Should().ThrowAsync<SecurityAccessPolicyConcurrencyException>();
    }

    [PostgresFact]
    public async Task Concurrent_updates_allow_one_expected_version_and_reject_the_stale_writer()
    {
        string connectionString = ConnectionString();

        DbContextOptions<AuthenticationDbContext> options = await PrepareAsync(connectionString);
        // Startup initialization stores the default policy at version 0, which both writers then edit.
        await using (AuthenticationDbContext seed = new(options))
            await new SecurityAccessPolicyStore(seed).InitializeAsync(CancellationToken.None);

        Task<SecurityAccessPolicyState> first = SaveAsync(options, SecurityAccessPolicy.Default with { FailedAttemptThreshold = 3 });
        Task<SecurityAccessPolicyState> second = SaveAsync(options, SecurityAccessPolicy.Default with { FailedAttemptThreshold = 4 });
        (SecurityAccessPolicyState? result, Exception? error)[] outcomes = await Task.WhenAll(Capture(first), Capture(second));

        outcomes.Count(item => item.result is not null).Should().Be(1);
        outcomes.Count(item => item.error is SecurityAccessPolicyConcurrencyException).Should().Be(1);
    }

    [PostgresFact]
    public async Task Postgres_concurrency_token_rejects_two_updates_loaded_from_the_same_version()
    {
        string connectionString = ConnectionString();

        DbContextOptions<AuthenticationDbContext> options = await PrepareAsync(connectionString);
        await using (AuthenticationDbContext seed = new(options))
            _ = await new SecurityAccessPolicyStore(seed).SaveAsync(SecurityAccessPolicy.Default, 0, CancellationToken.None);

        await using AuthenticationDbContext first = new(options);
        await using AuthenticationDbContext second = new(options);
        SecurityAccessPolicyRecord firstRecord = await first.SecurityAccessPolicies.SingleAsync();
        SecurityAccessPolicyRecord secondRecord = await second.SecurityAccessPolicies.SingleAsync();
        firstRecord.Apply(SecurityAccessPolicy.Default with { FailedAttemptThreshold = 3 });
        secondRecord.Apply(SecurityAccessPolicy.Default with { FailedAttemptThreshold = 4 });

        Exception?[] errors = await Task.WhenAll(
            CaptureConcurrencyAsync(first.SaveChangesAsync()),
            CaptureConcurrencyAsync(second.SaveChangesAsync()));

        errors.Count(error => error is null).Should().Be(1);
        errors.Count(error => error is DbUpdateConcurrencyException).Should().Be(1);
    }

    [PostgresFact]
    public async Task Concurrent_initial_inserts_allow_one_winner_and_translate_the_unique_constraint()
    {
        string connectionString = ConnectionString();

        DbContextOptions<AuthenticationDbContext> options = await PrepareAsync(connectionString);
        Task<SecurityAccessPolicyState> first = SaveAsync(options, SecurityAccessPolicy.Default, 0);
        Task<SecurityAccessPolicyState> second = SaveAsync(options, SecurityAccessPolicy.ProductEvaluated, 0);
        (SecurityAccessPolicyState? result, Exception? error)[] outcomes = await Task.WhenAll(Capture(first), Capture(second));

        outcomes.Count(item => item.result is not null).Should().Be(1);
        outcomes.Count(item => item.error is SecurityAccessPolicyConcurrencyException).Should().Be(1);
    }

    [PostgresFact]
    public async Task Existing_policy_stays_profile_independent_while_runtime_applies_profile_restrictions()
    {
        string connectionString = ConnectionString();

        DbContextOptions<AuthenticationDbContext> options = await PrepareAsync(connectionString);
        await using (AuthenticationDbContext seed = new(options))
            _ = await new SecurityAccessPolicyStore(seed).SaveAsync(SecurityAccessPolicy.Default, 0, CancellationToken.None);

        await using (AuthenticationDbContext context = new(options))
        {
            SecurityAccessPolicyStore store = new(context, AuthenticationProfilePolicy.ProductEvaluated);
            await store.InitializeAsync(CancellationToken.None);

            IAuthenticationSecurityPolicyProvider provider = store;
            SecurityAccessPolicy effective = await provider.GetAsync(CancellationToken.None);
            effective.Allows(AuthenticationMethod.Phone, PreAuthenticationAction.PasswordPlusPhoneOtp).Should().BeTrue();
            effective.Allows(AuthenticationMethod.Passkey, PreAuthenticationAction.PasskeyLogin).Should().BeFalse();
        }

        await using AuthenticationDbContext verification = new(options);
        SecurityAccessPolicy persisted = (await verification.SecurityAccessPolicies.SingleAsync()).ToDomain();
        persisted.Allows(AuthenticationMethod.Phone, PreAuthenticationAction.PasswordPlusPhoneOtp).Should().BeFalse();
        persisted.Allows(AuthenticationMethod.Passkey, PreAuthenticationAction.PasskeyLogin).Should().BeTrue();
    }

    [Fact]
    public async Task Profile_restrictions_are_runtime_only_and_reverse_when_the_profile_changes()
    {
        string databaseName = "authentication-profile-transition-" + Guid.NewGuid().ToString("N");
        DbContextOptions<AuthenticationDbContext> options = new DbContextOptionsBuilder<AuthenticationDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        await using (AuthenticationDbContext evaluatedContext = new(options))
        {
            SecurityAccessPolicyStore store = new(evaluatedContext, AuthenticationProfilePolicy.ProductEvaluated);
            await store.InitializeAsync(CancellationToken.None);

            IAuthenticationSecurityPolicyProvider provider = store;
            (await provider.GetAsync(CancellationToken.None))
                .Allows(AuthenticationMethod.Passkey, PreAuthenticationAction.PasskeyLogin)
                .Should().BeFalse();
        }

        await using AuthenticationDbContext defaultContext = new(options);
        SecurityAccessPolicyStore defaultStore = new(defaultContext, AuthenticationProfilePolicy.Default);
        IAuthenticationSecurityPolicyProvider defaultProvider = defaultStore;

        (await defaultProvider.GetAsync(CancellationToken.None))
            .Allows(AuthenticationMethod.Passkey, PreAuthenticationAction.PasskeyLogin)
            .Should().BeTrue();
    }

    private static async Task<DbContextOptions<AuthenticationDbContext>> PrepareAsync(string connectionString)
    {
        DbContextOptions<AuthenticationDbContext> options = new DbContextOptionsBuilder<AuthenticationDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        await using AuthenticationDbContext setup = new(options);
        await setup.Database.MigrateAsync();
        await setup.SecurityAccessPolicies.ExecuteDeleteAsync();
        return options;
    }

    private static Task<SecurityAccessPolicyState> SaveAsync(
        DbContextOptions<AuthenticationDbContext> options,
        SecurityAccessPolicy policy,
        long expectedVersion = 0) => SaveCoreAsync(options, policy, expectedVersion);

    private static async Task<SecurityAccessPolicyState> SaveCoreAsync(
        DbContextOptions<AuthenticationDbContext> options,
        SecurityAccessPolicy policy,
        long expectedVersion)
    {
        await using AuthenticationDbContext context = new(options);
        return await new SecurityAccessPolicyStore(context).SaveAsync(policy, expectedVersion, CancellationToken.None);
    }

    private static async Task<(SecurityAccessPolicyState? result, Exception? error)> Capture(Task<SecurityAccessPolicyState> operation)
    {
        try
        {
            return (await operation, null);
        }
        catch (Exception exception)
        {
            return (null, exception);
        }
    }

    private static async Task<Exception?> CaptureConcurrencyAsync(Task operation)
    {
        try
        {
            await operation;
            return null;
        }
        catch (Exception exception)
        {
            return exception;
        }
    }

    private static string ConnectionString() =>
        Environment.GetEnvironmentVariable("VITARISH_AUTH_TEST_CONNECTION")!;

    private sealed class PostgresFactAttribute : FactAttribute
    {
        public PostgresFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("VITARISH_AUTH_TEST_CONNECTION")))
                Skip = "PostgreSQL integration coverage requires VITARISH_AUTH_TEST_CONNECTION.";
        }
    }
}
