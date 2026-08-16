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

public sealed class SecurityAuditWriterTests
{
    [Fact]
    public async Task Persists_once_across_fresh_contexts_and_returns_existing_result_for_a_retry()
    {
        DbContextOptions<AuditDbContext> options = CreateOptions("security-audit-restart");
        await using (AuditDbContext setup = CreateContext(options, "tenant-a"))
        {
            await setup.Database.EnsureCreatedAsync();
        }

        SecurityAuditEvent first = CreateEvent("tenant-a", "request-1");
        await using (AuditDbContext initial = CreateContext(options, "tenant-a"))
        {
            SecurityAuditWriter writer = new(initial);
            (await writer.WriteAsync(first, CancellationToken.None)).Should().Be(SecurityAuditWriteResult.Recorded);
        }

        await using (AuditDbContext restarted = CreateContext(options, "tenant-a"))
        {
            SecurityAuditWriter writer = new(restarted);
            (await writer.WriteAsync(CreateEvent("tenant-a", "request-1"), CancellationToken.None)).Should().Be(SecurityAuditWriteResult.AlreadyRecorded);
            (await restarted.SecurityAuditEvents.CountAsync()).Should().Be(1);
        }
    }

    [Fact]
    public async Task Query_is_tenant_bound_after_a_store_restart()
    {
        DbContextOptions<AuditDbContext> options = CreateOptions("security-audit-tenants");
        await using (AuditDbContext setup = CreateContext(options, "tenant-a"))
        {
            await setup.Database.EnsureCreatedAsync();
        }

        await using (AuditDbContext tenantA = CreateContext(options, "tenant-a"))
        {
            _ = await new SecurityAuditWriter(tenantA).WriteAsync(CreateEvent("tenant-a", "request-a"), CancellationToken.None);
        }
        await using (AuditDbContext tenantB = CreateContext(options, "tenant-b"))
        {
            _ = await new SecurityAuditWriter(tenantB).WriteAsync(CreateEvent("tenant-b", "request-b"), CancellationToken.None);
        }

        await using AuditDbContext restartedTenantA = CreateContext(options, "tenant-a", [IdentityDefinitions.PlatformAdmin]);
        SecurityAuditQueryService query = new(restartedTenantA, new TestRequestContext("tenant-a", [IdentityDefinitions.PlatformAdmin]));
        IReadOnlyList<SecurityAuditEventReadDto> events = await query.GetByResourceAsync("account/42", CancellationToken.None);
        events.Should().ContainSingle().Which.TenantId.Should().Be("tenant-a");
        (await query.GetByIdempotencyKeyAsync("request-a", CancellationToken.None)).Should().NotBeNull();
        (await query.GetByIdempotencyKeyAsync("request-b", CancellationToken.None)).Should().BeNull();

        await using AuditDbContext ownerContext = CreateContext(options, "tenant-a", ["tenant-a:" + IdentityDefinitions.TenantOwner]);
        IReadOnlyList<SecurityAuditEventReadDto> ownerEvents = await new SecurityAuditQueryService(
            ownerContext,
            new TestRequestContext("tenant-a", ["tenant-a:" + IdentityDefinitions.TenantOwner]))
            .GetByResourceAsync("account/42", CancellationToken.None);
        ownerEvents.Should().ContainSingle();
    }

    [Fact]
    public async Task Rejects_a_non_utc_or_incomplete_event_before_persisting()
    {
        DbContextOptions<AuditDbContext> options = CreateOptions("security-audit-validation");
        await using AuditDbContext context = CreateContext(options, "tenant-a");
        await context.Database.EnsureCreatedAsync();

        SecurityAuditEvent invalid = CreateEvent("tenant-a", "request-1");
        invalid.OccurredAt = DateTime.SpecifyKind(invalid.OccurredAt, DateTimeKind.Local);
        Func<Task> write = () => new SecurityAuditWriter(context).WriteAsync(invalid, CancellationToken.None);

        await write.Should().ThrowAsync<ArgumentException>();
        (await context.SecurityAuditEvents.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Rejects_audit_review_without_a_tenant_owner_or_platform_admin_role()
    {
        DbContextOptions<AuditDbContext> options = CreateOptions("security-audit-authorization");
        await using AuditDbContext context = CreateContext(options, "tenant-a");
        await context.Database.EnsureCreatedAsync();

        Func<Task> read = () => new SecurityAuditQueryService(context, new TestRequestContext("tenant-a")).GetByResourceAsync("account/42", CancellationToken.None);

        await read.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Existing_entity_audit_remains_readable_to_an_authorized_tenant_owner()
    {
        DbContextOptions<AuditDbContext> options = CreateOptions("entity-audit-compatibility");
        await using (AuditDbContext context = CreateContext(options, "tenant-a", [$"tenant-a:{IdentityDefinitions.TenantOwner}"]))
        {
            await context.Database.EnsureCreatedAsync();
            context.AuditEntries.Add(new AuditEntry
            {
                Id = Guid.NewGuid(),
                Action = AuditAction.Updated,
                EntityType = "Patient",
                EntityId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                TenantId = "tenant-a",
                OccurredAt = DateTime.UtcNow,
                SnapshotAfter = "{\"status\":\"active\"}"
            });
            await context.SaveChangesAsync();
        }

        await using AuditDbContext readContext = CreateContext(options, "tenant-a", [$"tenant-a:{IdentityDefinitions.TenantOwner}"]);
        IReadOnlyList<AuditEntryReadDto> entries = await new AuditQueryService(readContext, new TestRequestContext("tenant-a", [$"tenant-a:{IdentityDefinitions.TenantOwner}"]))
            .GetByEntityAsync("Patient", Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), CancellationToken.None);

        entries.Should().ContainSingle().Which.SnapshotAfter.Should().Be("{\"status\":\"active\"}");
    }

    [Fact]
    public async Task Cancellation_is_not_converted_to_a_successful_audit_write()
    {
        DbContextOptions<AuditDbContext> options = CreateOptions("security-audit-cancellation");
        await using AuditDbContext context = CreateContext(options, "tenant-a");
        await context.Database.EnsureCreatedAsync();
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        Func<Task> write = () => new SecurityAuditWriter(context).WriteAsync(CreateEvent("tenant-a", "cancelled"), cancellation.Token);

        await write.Should().ThrowAsync<OperationCanceledException>();
        (await context.SecurityAuditEvents.CountAsync()).Should().Be(0);
    }

    private static AuditDbContext CreateContext(DbContextOptions<AuditDbContext> options, string tenantId) => new(options, new TestRequestContext(tenantId));

    private static AuditDbContext CreateContext(DbContextOptions<AuditDbContext> options, string tenantId, string[] roles) => new(options, new TestRequestContext(tenantId, roles));

    private static DbContextOptions<AuditDbContext> CreateOptions(string databaseName) =>
        new DbContextOptionsBuilder<AuditDbContext>().UseInMemoryDatabase(databaseName).Options;

    private static SecurityAuditEvent CreateEvent(string tenantId, string idempotencyKey) => new()
    {
        EventName = "authentication.password-login",
        Outcome = SecurityAuditOutcome.Succeeded,
        SubjectId = "user-42",
        TenantId = tenantId,
        Resource = "account/42",
        Operation = "Authentication_Login_Password",
        OccurredAt = DateTime.UtcNow,
        CorrelationId = idempotencyKey,
        SourceBoundary = "authentication.api",
        IdempotencyKey = idempotencyKey
    };

    private sealed class TestRequestContext(string tenantId, string[]? roles = null) : IRequestContext
    {
        public string Authority => "test";
        public string? ClientId => null;
        public string? OwnerId => null;
        public string? UserId => null;
        public string? TenantId => tenantId;
        public string? ResourceId => null;
        public string? TraceId => null;
        public string? RequestId => null;
        public string? AdditionalInfo => null;
        public ClaimsPrincipal? User => new(new ClaimsIdentity((roles ?? []).Select(role => new Claim(ClaimTypes.Role, role)), "test"));
        public Request GetRequest() => new() { Authority = Authority, TenantId = TenantId };
    }
}
