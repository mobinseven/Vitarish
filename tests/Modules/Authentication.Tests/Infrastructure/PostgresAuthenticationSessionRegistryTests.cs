using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Domain.Security;
using Vitarish.Authentication.Infrastructure;
using Vitarish.Requests.Application.Abstractions;
using Xunit;

namespace Authentication.Tests.Infrastructure;

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
