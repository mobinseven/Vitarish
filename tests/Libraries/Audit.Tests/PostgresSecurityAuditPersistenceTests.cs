using System.Security.Claims;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Audit.Server;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Requests.Domain.Entities;
using Xunit;

namespace Vitarish.Audit.Tests;

public sealed class PostgresSecurityAuditPersistenceTests
{
    [Fact]
    public async Task Event_survives_a_fresh_postgres_context()
    {
        string? connectionString = Environment.GetEnvironmentVariable("AUDIT_TEST_CONNECTION");
        if (string.IsNullOrWhiteSpace(connectionString))
            return;

        DbContextOptions<AuditDbContext> options = new DbContextOptionsBuilder<AuditDbContext>().UseNpgsql(connectionString).Options;
        await using (AuditDbContext setup = CreateContext(options))
        {
            await setup.Database.MigrateAsync();
        }

        string key = Environment.GetEnvironmentVariable("AUDIT_TEST_KEY") ?? ("postgres-restart-" + Guid.NewGuid().ToString("N"));
        bool expectExisting = string.Equals(Environment.GetEnvironmentVariable("AUDIT_TEST_EXPECT_EXISTING"), "true", StringComparison.OrdinalIgnoreCase);
        await using (AuditDbContext first = CreateContext(options))
        {
            SecurityAuditWriteResult result = await new SecurityAuditWriter(first).WriteAsync(CreateEvent(key), CancellationToken.None);
            result.Should().Be(expectExisting ? SecurityAuditWriteResult.AlreadyRecorded : SecurityAuditWriteResult.Recorded);
        }

        await using AuditDbContext second = CreateContext(options);
        SecurityAuditEvent? persisted = await second.SecurityAuditEvents.AsNoTracking().SingleOrDefaultAsync(e => e.IdempotencyKey == key);
        persisted.Should().NotBeNull();
        persisted!.TenantId.Should().Be("audit-test-tenant");
    }

    [Fact]
    public async Task Store_outage_is_exposed_as_a_mandatory_audit_failure()
    {
        string? connectionString = Environment.GetEnvironmentVariable("AUDIT_TEST_FAILURE_CONNECTION");
        if (string.IsNullOrWhiteSpace(connectionString))
            return;

        DbContextOptions<AuditDbContext> options = new DbContextOptionsBuilder<AuditDbContext>().UseNpgsql(connectionString).Options;
        await using AuditDbContext context = CreateContext(options);
        Func<Task> write = () => new SecurityAuditWriter(context).WriteAsync(CreateEvent("outage-" + Guid.NewGuid().ToString("N")), CancellationToken.None);

        await write.Should().ThrowAsync<SecurityAuditStoreException>();
    }

    [Fact]
    public async Task Store_recovers_after_an_outage_without_replaying_the_failed_attempt()
    {
        string? outageConnection = Environment.GetEnvironmentVariable("AUDIT_TEST_FAILURE_CONNECTION");
        string? recoveryConnection = Environment.GetEnvironmentVariable("AUDIT_TEST_RECOVERY_CONNECTION");
        if (string.IsNullOrWhiteSpace(outageConnection) || string.IsNullOrWhiteSpace(recoveryConnection))
            return;

        DbContextOptions<AuditDbContext> outageOptions = new DbContextOptionsBuilder<AuditDbContext>().UseNpgsql(outageConnection).Options;
        await using (AuditDbContext outage = CreateContext(outageOptions))
        {
            Func<Task> write = () => new SecurityAuditWriter(outage).WriteAsync(CreateEvent("recovery-outage-" + Guid.NewGuid().ToString("N")), CancellationToken.None);
            await write.Should().ThrowAsync<SecurityAuditStoreException>();
        }

        DbContextOptions<AuditDbContext> recoveryOptions = new DbContextOptionsBuilder<AuditDbContext>().UseNpgsql(recoveryConnection).Options;
        await using (AuditDbContext setup = CreateContext(recoveryOptions))
        {
            await setup.Database.MigrateAsync();
        }

        string key = "recovery-success-" + Guid.NewGuid().ToString("N");
        await using AuditDbContext recovered = CreateContext(recoveryOptions);
        (await new SecurityAuditWriter(recovered).WriteAsync(CreateEvent(key), CancellationToken.None))
            .Should().Be(SecurityAuditWriteResult.Recorded);
        (await recovered.SecurityAuditEvents.AsNoTracking().AnyAsync(e => e.IdempotencyKey == key)).Should().BeTrue();
    }

    private static AuditDbContext CreateContext(DbContextOptions<AuditDbContext> options) => new(options, new TestRequestContext());

    private static SecurityAuditEvent CreateEvent(string key) => new()
    {
        EventName = "audit.persistence.test",
        EventCategory = SecurityAuditEventCatalog.ManagementCategory,
        Outcome = SecurityAuditOutcome.Succeeded,
        TenantId = "audit-test-tenant",
        Operation = "audit.persistence.test",
        OccurredAt = DateTime.UtcNow,
        CorrelationId = key,
        SourceBoundary = "audit.tests",
        IdempotencyKey = key
    };

    private sealed class TestRequestContext : IRequestContext
    {
        public string Authority => "test";
        public string? ClientId => null;
        public string? OwnerId => null;
        public string? UserId => null;
        public string? TenantId => "audit-test-tenant";
        public string? ResourceId => null;
        public string? TraceId => null;
        public string? RequestId => null;
        public string? AdditionalInfo => null;
        public ClaimsPrincipal? User => null;
        public Request GetRequest() => new() { RequestId = "audit-test", Authority = Authority, TenantId = TenantId };
    }
}
