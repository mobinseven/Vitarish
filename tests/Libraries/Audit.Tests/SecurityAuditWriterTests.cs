using System.Data.Common;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
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

    [Fact]
    public async Task A_refused_write_still_fails_and_counts_toward_the_outage_summary()
    {
        RecordingOutageCounter counter = new();
        await using AuditDbContext context = CreateUnavailableContext("security-audit-outage-count");

        Func<Task> write = () => new SecurityAuditWriter(context, outageCounter: counter).WriteAsync(CreateEvent("tenant-a", "refused"), CancellationToken.None);

        await write.Should().ThrowAsync<SecurityAuditStoreException>();
        counter.FailedWrites.Should().ContainSingle().Which.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Theory]
    [InlineData("23514")]
    [InlineData("22001")]
    [InlineData("42501")]
    public async Task An_event_rejected_by_the_store_does_not_count_toward_the_outage(string sqlState)
    {
        RecordingOutageCounter counter = new();
        await using AuditDbContext context = CreateFailingContext(
            $"security-audit-rejected-{sqlState}",
            new SqlStateException(sqlState));

        Func<Task> write = () => new SecurityAuditWriter(context, outageCounter: counter).WriteAsync(CreateEvent("tenant-a", "rejected"), CancellationToken.None);

        await write.Should().ThrowAsync<SecurityAuditStoreException>();
        counter.FailedWrites.Should().BeEmpty("a rejected event is not an unavailable store");
    }

    [Fact]
    public async Task The_outage_summary_write_does_not_count_toward_the_outage()
    {
        RecordingOutageCounter counter = new();
        await using AuditDbContext context = CreateUnavailableContext("security-audit-outage-summary");

        Func<Task> write = () => new SecurityAuditWriter(context, outageCounter: counter).WriteAsync(
            CreateOutageSummaryEvent("tenant-a", "outage-summary"),
            CancellationToken.None);

        await write.Should().ThrowAsync<SecurityAuditStoreException>();
        counter.FailedWrites.Should().BeEmpty("the summary describes the outage and must not inflate it");
    }

    [Fact]
    public async Task An_unavailable_outage_counter_does_not_change_the_write_failure()
    {
        RecordingOutageCounter counter = new() { Unavailable = true };
        await using AuditDbContext context = CreateUnavailableContext("security-audit-outage-counter-down");

        Func<Task> write = () => new SecurityAuditWriter(context, outageCounter: counter).WriteAsync(CreateEvent("tenant-a", "refused"), CancellationToken.None);

        await write.Should().ThrowAsync<SecurityAuditStoreException>();
    }

    [Fact]
    public async Task A_successful_write_does_not_touch_the_outage_counter()
    {
        RecordingOutageCounter counter = new();
        DbContextOptions<AuditDbContext> options = CreateOptions("security-audit-outage-success");
        await using AuditDbContext context = CreateContext(options, "tenant-a");
        await context.Database.EnsureCreatedAsync();

        (await new SecurityAuditWriter(context, outageCounter: counter).WriteAsync(CreateEvent("tenant-a", "stored"), CancellationToken.None))
            .Should().Be(SecurityAuditWriteResult.Recorded);
        counter.FailedWrites.Should().BeEmpty();
    }

    [Fact]
    public void PostgreSql_capacity_query_aliases_the_scalar_and_counts_indexes_once()
    {
        PostgresAuditStorageSizeReader.StorageSizeSql.Should().Contain("pg_table_size");
        PostgresAuditStorageSizeReader.StorageSizeSql.Should().Contain("pg_indexes_size");
        PostgresAuditStorageSizeReader.StorageSizeSql.Should().Contain("AS \"Value\"");
        PostgresAuditStorageSizeReader.StorageSizeSql.Should().NotContain("pg_total_relation_size");
    }

    [Fact]
    public async Task Pending_outcome_can_be_completed_idempotently_after_a_store_restart()
    {
        DbContextOptions<AuditDbContext> options = CreateOptions("security-audit-pending-completion");
        await using (AuditDbContext setup = CreateContext(options, "tenant-a"))
        {
            await setup.Database.EnsureCreatedAsync();
        }

        SecurityAuditEvent pending = CreateEvent("tenant-a", "request-pending");
        pending.Outcome = SecurityAuditOutcome.Pending;
        await using (AuditDbContext initial = CreateContext(options, "tenant-a"))
        {
            SecurityAuditWriter writer = new(initial);
            _ = await writer.WriteAsync(pending, CancellationToken.None);
        }

        await using (AuditDbContext restarted = CreateContext(options, "tenant-a"))
        {
            SecurityAuditWriter writer = new(restarted);
            (writer as ISecurityAuditOutcomeUpdater).Should().NotBeNull();
            (await writer.UpdateAsync("tenant-a", "request-pending", SecurityAuditOutcome.Succeeded, "version:1", CancellationToken.None))
                .Should().Be(SecurityAuditWriteResult.Recorded);
            (await writer.UpdateAsync("tenant-a", "request-pending", SecurityAuditOutcome.Succeeded, "version:1", CancellationToken.None))
                .Should().Be(SecurityAuditWriteResult.AlreadyRecorded);
            (await restarted.SecurityAuditEvents.CountAsync()).Should().Be(2);
            (await restarted.SecurityAuditEvents.SingleAsync(item => item.Outcome == SecurityAuditOutcome.Pending)).Outcome.Should().Be(SecurityAuditOutcome.Pending);
            (await restarted.SecurityAuditEvents.SingleAsync(item => item.Outcome == SecurityAuditOutcome.Succeeded)).Outcome.Should().Be(SecurityAuditOutcome.Succeeded);
        }
    }

    [Fact]
    public async Task Event_for_another_tenant_is_recorded_in_that_tenants_scope()
    {
        DbContextOptions<AuditDbContext> options = CreateOptions("security-audit-row-scope");
        await using (AuditDbContext onboarding = CreateContext(options, "onboarding"))
        {
            (await new SecurityAuditWriter(onboarding).WriteAsync(CreateEvent("clinic-b", "created-clinic-b"), CancellationToken.None))
                .Should().Be(SecurityAuditWriteResult.Recorded);
        }

        await using AuditDbContext clinic = CreateContext(options, "clinic-b");
        (await clinic.SecurityAuditEvents.SingleAsync()).IdempotencyKey.Should().Be("created-clinic-b");
        await using AuditDbContext requestTenant = CreateContext(options, "onboarding");
        (await requestTenant.SecurityAuditEvents.AnyAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task Platform_event_from_a_request_without_a_tenant_is_recorded()
    {
        DbContextOptions<AuditDbContext> options = CreateOptions("security-audit-tenantless");
        await using (AuditDbContext tenantless = CreateContext(options, tenantId: null))
        {
            SecurityAuditWriter writer = new(tenantless);
            (await writer.WriteAsync(CreateEvent(SecurityAuditScopeCatalog.PlatformScopeId, "sign-in-on-entry-host"), CancellationToken.None))
                .Should().Be(SecurityAuditWriteResult.Recorded);
            (await writer.GetOutcomeAsync(SecurityAuditScopeCatalog.PlatformScopeId, "sign-in-on-entry-host", CancellationToken.None))
                .Should().Be(SecurityAuditOutcome.Succeeded);
        }

        await using AuditDbContext platform = CreateContext(options, SecurityAuditScopeCatalog.PlatformScopeId);
        (await platform.SecurityAuditEvents.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Entity_audit_batch_stores_each_entry_in_the_scope_of_its_entity()
    {
        DbContextOptions<AuditDbContext> options = CreateOptions("entity-audit-row-scope");
        await using (AuditDbContext onboarding = CreateContext(options, "onboarding"))
        {
            await new AuditWriter(onboarding).WriteBatchAsync(
                [CreateEntry("clinic-b"), CreateEntry("onboarding"), CreateEntry("clinic-b")],
                CancellationToken.None);
        }

        await using AuditDbContext clinic = CreateContext(options, "clinic-b");
        (await clinic.AuditEntries.CountAsync()).Should().Be(2);
        await using AuditDbContext requestTenant = CreateContext(options, "onboarding");
        (await requestTenant.AuditEntries.CountAsync()).Should().Be(1);
    }

    private static AuditEntry CreateEntry(string tenantId) => new()
    {
        Action = AuditAction.Created,
        EntityType = "Tenant",
        EntityId = Guid.NewGuid(),
        OccurredAt = DateTime.UtcNow,
        TenantId = tenantId
    };

    private static AuditDbContext CreateContext(DbContextOptions<AuditDbContext> options, string? tenantId) => new(options, new TestRequestContext(tenantId));

    private static AuditDbContext CreateContext(DbContextOptions<AuditDbContext> options, string tenantId, string[] roles) => new(options, new TestRequestContext(tenantId, roles));

    private static DbContextOptions<AuditDbContext> CreateOptions(string databaseName) =>
        new DbContextOptionsBuilder<AuditDbContext>().UseInMemoryDatabase(databaseName).Options;

    private static AuditDbContext CreateUnavailableContext(string databaseName) =>
        CreateFailingContext(databaseName, new SqlStateException("08006"));

    private static AuditDbContext CreateFailingContext(string databaseName, Exception failure)
    {
        DbContextOptions<AuditDbContext> options = new DbContextOptionsBuilder<AuditDbContext>()
            .UseInMemoryDatabase(databaseName)
            .AddInterceptors(new RefusingSaveInterceptor(failure))
            .Options;
        return CreateContext(options, "tenant-a");
    }

    private sealed class RefusingSaveInterceptor(Exception failure) : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default) =>
            throw new DbUpdateException("The audit store refused the write.", failure);
    }

    private sealed class SqlStateException(string sqlState) : DbException("The audit store is unavailable.")
    {
        public override string? SqlState => sqlState;
    }

    private sealed class RecordingOutageCounter : IAuditStorageOutageCounter
    {
        public bool Unavailable { get; init; }

        public List<DateTime> FailedWrites { get; } = [];

        public Task RecordFailedWriteAsync(DateTime failedAt, CancellationToken cancellationToken)
        {
            if (Unavailable)
                throw new InvalidOperationException("The outage counter store is unavailable.");
            FailedWrites.Add(failedAt);
            return Task.CompletedTask;
        }

        public Task<AuditStorageOutage?> GetAsync(CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<bool> ClearAsync(AuditStorageOutage summarized, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private static SecurityAuditEvent CreateEvent(string tenantId, string idempotencyKey) => new()
    {
        EventName = "authentication.password-login",
        EventCategory = SecurityAuditEventCatalog.AuthenticationCategory,
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

    private static SecurityAuditEvent CreateOutageSummaryEvent(string tenantId, string idempotencyKey) => new()
    {
        EventName = SecurityAuditEventCatalog.SecurityFailureEvent,
        EventCategory = SecurityAuditEventCatalog.SecurityFailureCategory,
        Outcome = SecurityAuditOutcome.Failed,
        ActorKind = SecurityAuditActorKind.System,
        TenantId = tenantId,
        Resource = SecurityAuditScopeCatalog.PlatformResource,
        Operation = SecurityAuditEventCatalog.SecurityFailureOperation,
        OccurredAt = DateTime.UtcNow,
        CorrelationId = idempotencyKey,
        SourceBoundary = SecurityAuditEventCatalog.AuditStorageOutageSourceBoundary,
        SourceIpKind = SecurityAuditSourceKind.System,
        IdempotencyKey = idempotencyKey
    };

    private sealed class TestRequestContext(string? tenantId, string[]? roles = null) : IRequestContext
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
