using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Domain.Security;
using Vitarish.Authentication.Domain.Sessions;
using Vitarish.Authentication.Infrastructure;
using Vitarish.Requests.Application.Abstractions;
using Xunit;

namespace Authentication.Tests.Infrastructure;

[Collection(AuthenticationPostgresCollection.Name)]
public sealed class PostgresAuthenticationSessionRegistryTests
{
    [PostgresFact]
    public async Task Retry_enabled_postgres_establishment_commits_the_session_transaction()
    {
        string connectionString = ConnectionString();
        DbContextOptions<AuthenticationDbContext> options = await PrepareAsync(connectionString);
        await using AuthenticationDbContext context = new(options);
        SecurityAccessPolicyStore policyStore = new(context);
        await policyStore.InitializeAsync(CancellationToken.None);

        Mock<IClientIpAddressAccessor> clientIp = new();
        clientIp.SetupGet(accessor => accessor.ClientIpAddress).Returns("127.0.0.1");
        Mock<IRequestContext> requestContext = new();
        requestContext.SetupGet(value => value.TenantId).Returns("tenant-1");
        requestContext.SetupGet(value => value.UserId).Returns("user-1");
        requestContext.SetupGet(value => value.RequestId).Returns("postgres-session-test");
        requestContext.SetupGet(value => value.TraceId).Returns("postgres-session-trace");

        AuthenticationSessionRegistry registry = new(
            context,
            policyStore,
            clientIp.Object,
            requestContext.Object,
            new RecordingAuditWriter(),
            TimeProvider.System);

        AuthenticationSessionEstablishmentResult result = await registry.EstablishAsync("user-1", CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        (await context.AuthenticationSessions.CountAsync(session => session.UserId == "user-1" && session.TerminatedAt == null))
            .Should().Be(1);
        (await context.AuthenticationAccessHistories.CountAsync(history => history.UserId == "user-1"))
            .Should().Be(1);
    }

    [PostgresFact]
    public async Task Stale_policy_version_does_not_revoke_sessions_or_change_the_policy()
    {
        string connectionString = ConnectionString();
        DbContextOptions<AuthenticationDbContext> options = await PrepareAsync(connectionString);
        await using AuthenticationDbContext registryContext = new(options);
        await using AuthenticationDbContext storeContext = new(options);
        SecurityAccessPolicyStore policyStore = new(storeContext);
        await policyStore.InitializeAsync(CancellationToken.None);
        AuthenticationSession session = new()
        {
            Id = Guid.NewGuid(),
            UserId = "policy-atomicity-user",
            TenantId = "tenant-1",
            CreatedAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow,
            ClientIpAddress = "127.0.0.1"
        };
        registryContext.AuthenticationSessions.Add(session);
        await registryContext.SaveChangesAsync();
        AuthenticationSessionRegistry registry = CreateSessionRegistry(registryContext);
        SecurityAccessPolicy changed = SecurityAccessPolicy.Default with
        {
            AllowedSessionStartCidrs = new HashSet<string>(StringComparer.Ordinal) { "10.0.0.0/24" }
        };

        Func<Task> act = () => registry.SavePolicyAndRevokeInvalidatedSessionsAsync(changed, 5, CancellationToken.None);

        await act.Should().ThrowAsync<SecurityAccessPolicyConcurrencyException>();
        (await registryContext.AuthenticationSessions.AsNoTracking().SingleAsync(item => item.Id == session.Id).ConfigureAwait(false))
            .TerminatedAt.Should().BeNull();
        SecurityAccessPolicyState state = await policyStore.GetStateAsync(CancellationToken.None);
        state.Version.Should().Be(0);
        state.Policy.AllowedSessionStartCidrs.Should().BeNullOrEmpty();
    }

    [PostgresFact]
    public async Task Revocation_failure_rolls_back_the_policy_change_written_in_the_same_transaction()
    {
        string connectionString = ConnectionString();
        DbContextOptions<AuthenticationDbContext> options = await PrepareAsync(connectionString);
        await using AuthenticationDbContext registryContext = new(options);
        await using AuthenticationDbContext storeContext = new(options);
        SecurityAccessPolicyStore policyStore = new(storeContext);
        await policyStore.InitializeAsync(CancellationToken.None);
        AuthenticationSession session = new()
        {
            Id = Guid.NewGuid(),
            UserId = "policy-rollback-user",
            TenantId = "tenant-1",
            CreatedAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow,
            ClientIpAddress = "127.0.0.1"
        };
        registryContext.AuthenticationSessions.Add(session);
        await registryContext.SaveChangesAsync();

        await using (AuthenticationDbContext guard = new(options))
        {
            await guard.Database.ExecuteSqlRawAsync(
                """
                CREATE OR REPLACE FUNCTION "Authentication"."block_authentication_session_termination"() RETURNS trigger AS $block$
                BEGIN
                    RAISE EXCEPTION 'session termination blocked by atomicity test';
                END;
                $block$ LANGUAGE plpgsql;
                CREATE TRIGGER "block_authentication_session_termination" BEFORE UPDATE ON "Authentication"."AuthenticationSessions"
                FOR EACH ROW EXECUTE FUNCTION "Authentication"."block_authentication_session_termination"();
                """);
        }

        try
        {
            AuthenticationSessionRegistry registry = CreateSessionRegistry(registryContext);
            SecurityAccessPolicy changed = SecurityAccessPolicy.Default with
            {
                AllowedSessionStartCidrs = new HashSet<string>(StringComparer.Ordinal) { "10.0.0.0/24" }
            };

            Func<Task> act = () => registry.SavePolicyAndRevokeInvalidatedSessionsAsync(changed, 0, CancellationToken.None);

            await act.Should().ThrowAsync<SecurityAccessPolicyPersistenceException>();
        }
        finally
        {
            await using AuthenticationDbContext cleanup = new(options);
            await cleanup.Database.ExecuteSqlRawAsync(
                """
                DROP TRIGGER IF EXISTS "block_authentication_session_termination" ON "Authentication"."AuthenticationSessions";
                DROP FUNCTION IF EXISTS "Authentication"."block_authentication_session_termination"();
                """);
        }

        SecurityAccessPolicyState state = await policyStore.GetStateAsync(CancellationToken.None);
        state.Version.Should().Be(0);
        state.Policy.AllowedSessionStartCidrs.Should().BeNullOrEmpty();
        (await registryContext.AuthenticationSessions.AsNoTracking().SingleAsync(item => item.Id == session.Id).ConfigureAwait(false))
            .TerminatedAt.Should().BeNull();
    }

    private static AuthenticationSessionRegistry CreateSessionRegistry(AuthenticationDbContext context)
    {
        Mock<IAuthenticationSecurityPolicyProvider> policyProvider = new();
        policyProvider.Setup(provider => provider.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(SecurityAccessPolicy.Default);
        Mock<IClientIpAddressAccessor> clientIp = new();
        clientIp.SetupGet(accessor => accessor.ClientIpAddress).Returns("127.0.0.1");
        Mock<IRequestContext> requestContext = new();
        requestContext.SetupGet(value => value.TenantId).Returns("tenant-1");
        requestContext.SetupGet(value => value.UserId).Returns("admin-1");
        requestContext.SetupGet(value => value.RequestId).Returns("policy-atomicity-test");
        requestContext.SetupGet(value => value.TraceId).Returns("policy-atomicity-trace");

        return new AuthenticationSessionRegistry(
            context,
            policyProvider.Object,
            clientIp.Object,
            requestContext.Object,
            new RecordingAuditWriter(),
            TimeProvider.System);
    }

    private static async Task<DbContextOptions<AuthenticationDbContext>> PrepareAsync(string connectionString)
    {
        DbContextOptions<AuthenticationDbContext> options = new DbContextOptionsBuilder<AuthenticationDbContext>()
            .UseNpgsql(connectionString, postgres => postgres.EnableRetryOnFailure())
            .Options;
        await using AuthenticationDbContext setup = new(options);
        await setup.Database.MigrateAsync();
        await setup.AuthenticationSessionNotices.ExecuteDeleteAsync();
        await setup.AuthenticationSessions.ExecuteDeleteAsync();
        await setup.AuthenticationAccessHistories.ExecuteDeleteAsync();
        await setup.AuthenticationUserSessionPolicies.ExecuteDeleteAsync();
        await setup.SecurityAccessPolicies.ExecuteDeleteAsync();
        return options;
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

    private sealed class RecordingAuditWriter : ISecurityAuditWriter
    {
        public Task<SecurityAuditWriteResult> WriteAsync(SecurityAuditEvent auditEvent, CancellationToken cancellationToken) =>
            Task.FromResult(SecurityAuditWriteResult.Recorded);
    }
}
