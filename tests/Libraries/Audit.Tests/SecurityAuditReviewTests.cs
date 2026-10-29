using System.Security.Claims;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Audit.Server;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Requests.Domain.Entities;
using Xunit;

namespace Vitarish.Audit.Tests;

public sealed class SecurityAuditReviewTests
{
    [Fact]
    public async Task Writer_sets_explicit_actor_source_and_redacts_free_form_reasons()
    {
        DbContextOptions<AuditDbContext> options = CreateOptions();
        await using AuditDbContext context = CreateContext(options, "tenant-a");
        await context.Database.EnsureCreatedAsync();

        SecurityAuditEvent auditEvent = new()
        {
            EventName = SecurityAuditEventCatalog.AuthenticationPasswordLoginEvent,
            Outcome = SecurityAuditOutcome.Denied,
            ActorId = "user-a",
            TenantId = "tenant-a",
            Resource = "authentication",
            Operation = SecurityAuditEventCatalog.PasswordLoginOperation,
            OccurredAt = DateTime.UtcNow,
            CorrelationId = "request-a",
            SourceBoundary = "tests",
            RedactedReason = "patient payload with sensitive content",
            IdempotencyKey = "request-a"
        };

        _ = await new SecurityAuditWriter(context, new TestClientIpAddressAccessor("203.0.113.7"))
            .WriteAsync(auditEvent, CancellationToken.None);

        SecurityAuditEvent stored = await context.SecurityAuditEvents.SingleAsync();
        stored.EventCategory.Should().Be(SecurityAuditEventCatalog.AuthenticationCategory);
        stored.ActorKind.Should().Be(SecurityAuditActorKind.User);
        stored.SourceIpKind.Should().Be(SecurityAuditSourceKind.Client);
        stored.SourceIpAddress.Should().Be("203.0.113.7");
        stored.RedactedReason.Should().Be(SecurityAuditRedaction.Replacement);
    }

    [Theory]
    [InlineData("123456")]
    [InlineData("eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxMjMifQ.signature")]
    [InlineData("arbitrary_reason")]
    public void Writer_rejects_credential_shaped_and_unknown_reasons(string reason)
    {
        SecurityAuditRedaction.Apply(reason).Should().Be(SecurityAuditRedaction.Replacement);
    }

    [Fact]
    public void Audit_storage_outage_reason_is_kept_only_in_its_typed_form()
    {
        DateTime started = new(2026, 10, 25, 9, 0, 0, DateTimeKind.Utc);
        DateTime lastFailed = new(2026, 10, 25, 9, 42, 5, DateTimeKind.Utc);

        SecurityAuditRedaction.Apply(SecurityAuditReasonCatalog.AuditStorageOutage(started, lastFailed, 37))
            .Should().Be("audit_storage_outage:start=2026-10-25T09:00:00Z;end=2026-10-25T09:42:05Z;failed_writes=37");
        SecurityAuditRedaction.Apply("audit_storage_outage:start=2026-10-25T09:00:00Z;end=yesterday;failed_writes=37")
            .Should().Be(SecurityAuditRedaction.Replacement);
        SecurityAuditRedaction.Apply("audit_storage_outage:start=2026-10-25T09:00:00Z;end=2026-10-25T09:42:05Z;failed_writes=0")
            .Should().Be(SecurityAuditRedaction.Replacement);
        ((Action)(() => SecurityAuditReasonCatalog.AuditStorageOutage(lastFailed, started, 1))).Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Redaction_preserves_only_catalogued_reasons_and_typed_dynamic_reasons()
    {
        SecurityAuditRedaction.Apply(SecurityAuditReasonCatalog.PasswordLoginDisabled).Should().Be(SecurityAuditReasonCatalog.PasswordLoginDisabled);
        SecurityAuditRedaction.Apply(SecurityAuditReasonCatalog.HttpStatus(401)).Should().Be("http_401");
        SecurityAuditRedaction.Apply(SecurityAuditReasonCatalog.Version(8)).Should().Be("version:8");
        SecurityAuditRedaction.Apply(SecurityAuditReasonCatalog.UserTimeoutMinutes(15)).Should().Be("user_timeout_minutes:15");
        SecurityAuditRedaction.Apply(SecurityAuditReasonCatalog.AuditStorageThreshold(
                4096,
                1024,
                SecurityAuditReasonCatalog.NotificationChannelSucceeded,
                SecurityAuditReasonCatalog.NotificationChannelFailed))
            .Should().Be("audit_storage_threshold:bytes=4096;threshold=1024;email=succeeded;sms=failed");
        SecurityAuditRedaction.Apply(SecurityAuditReasonCatalog.AuditStorageThreshold(
                4096,
                1024,
                SecurityAuditReasonCatalog.NotificationChannelNotAttempted,
                SecurityAuditReasonCatalog.NotificationChannelNotAttempted))
            .Should().Be("audit_storage_threshold:bytes=4096;threshold=1024;email=not_attempted;sms=not_attempted");
        SecurityAuditRedaction.Apply(SecurityAuditReasonCatalog.AuditStorageThresholdWithoutRecipients(4096, 1024))
            .Should().Be("audit_storage_threshold:bytes=4096;threshold=1024;recipients=not_configured");
        SecurityAuditRedaction.Apply("audit_storage_threshold:bytes=4096;threshold=1024;email=succeeded")
            .Should().Be(SecurityAuditRedaction.Replacement);
        SecurityAuditRedaction.Apply("audit_storage_threshold:bytes=4096;threshold=0;email=succeeded;sms=succeeded")
            .Should().Be(SecurityAuditRedaction.Replacement);
    }

    [Fact]
    public void Catalog_covers_the_mandatory_event_categories_and_maps_administrator_session_termination()
    {
        SecurityAuditEventCatalog.Entries.Select(entry => entry.Category).Should().Contain(
            [
                SecurityAuditEventCatalog.FunctionCategory,
                SecurityAuditEventCatalog.ReviewCategory,
                SecurityAuditEventCatalog.ConfigurationCategory,
                SecurityAuditEventCatalog.AuthenticationCategory,
                SecurityAuditEventCatalog.SecurityAttributeCategory,
                SecurityAuditEventCatalog.ImportExportCategory,
                SecurityAuditEventCatalog.ManagementCategory,
                SecurityAuditEventCatalog.SecurityFailureCategory,
                SecurityAuditEventCatalog.SessionCategory
            ]);

        SecurityAuditCatalogEntry sessionTermination = SecurityAuditEventCatalog.Entries
            .Single(entry => entry.EventName == SecurityAuditEventCatalog.SessionTerminatedEvent);
        sessionTermination.ControlItems.Should().Contain(SecurityAuditControl.AdministratorIdleTermination);
        SecurityAuditEventCatalog.Find(SecurityAuditEventCatalog.EndpointMutationEvent, "any-operation")
            .Should().NotBeNull();

        SecurityAuditEventCatalog.IsCatalogued(new SecurityAuditEvent
        {
            EventName = SecurityAuditEventCatalog.EndpointMutationEvent,
            EventCategory = SecurityAuditEventCatalog.FunctionCategory,
            Operation = "any-operation"
        }).Should().BeTrue();
        SecurityAuditEventCatalog.IsCatalogued(new SecurityAuditEvent
        {
            EventName = SecurityAuditEventCatalog.SessionTerminatedEvent,
            EventCategory = SecurityAuditEventCatalog.SessionCategory,
            Operation = SecurityAuditEventCatalog.SessionTerminatedOperation
        }).Should().BeTrue();
        SecurityAuditEventCatalog.IsCatalogued(new SecurityAuditEvent
        {
            EventName = SecurityAuditEventCatalog.AuthenticationPasswordLoginEvent,
            EventCategory = SecurityAuditEventCatalog.ManagementCategory,
            Operation = SecurityAuditEventCatalog.PasswordLoginOperation
        }).Should().BeFalse();
    }

    [Fact]
    public void Catalogues_each_identity_sign_in_manager_operation_and_redacts_new_reasons()
    {
        SecurityAuditEventCatalog.Entries.Should().Contain(entry => entry.EventName == SecurityAuditEventCatalog.AuthenticationIdentityPasswordSignInUserEvent && entry.Operation == SecurityAuditEventCatalog.IdentityPasswordSignInUserOperation);
        SecurityAuditEventCatalog.Entries.Should().Contain(entry => entry.EventName == SecurityAuditEventCatalog.AuthenticationIdentityPasswordSignInNameEvent && entry.Operation == SecurityAuditEventCatalog.IdentityPasswordSignInNameOperation);
        SecurityAuditEventCatalog.Entries.Should().Contain(entry => entry.EventName == SecurityAuditEventCatalog.AuthenticationIdentityPasswordCheckEvent && entry.Operation == SecurityAuditEventCatalog.IdentityPasswordCheckOperation);
        SecurityAuditEventCatalog.Entries.Should().Contain(entry => entry.EventName == SecurityAuditEventCatalog.AuthenticationIdentityExternalLoginEvent && entry.Operation == SecurityAuditEventCatalog.IdentityExternalLoginOperation);
        SecurityAuditEventCatalog.Entries.Should().Contain(entry => entry.EventName == SecurityAuditEventCatalog.AuthenticationIdentityAuthenticatorMfaEvent && entry.Operation == SecurityAuditEventCatalog.IdentityAuthenticatorMfaOperation);
        SecurityAuditEventCatalog.Entries.Should().Contain(entry => entry.EventName == SecurityAuditEventCatalog.AuthenticationIdentityRecoveryCodeMfaEvent && entry.Operation == SecurityAuditEventCatalog.IdentityRecoveryCodeMfaOperation);
        SecurityAuditEventCatalog.Entries.Should().Contain(entry => entry.EventName == SecurityAuditEventCatalog.AuthenticationIdentityProviderMfaEvent && entry.Operation == SecurityAuditEventCatalog.IdentityProviderMfaOperation);

        SecurityAuditReasonCatalog.IsAllowed(SecurityAuditReasonCatalog.AuthenticationLockout).Should().BeTrue();
        SecurityAuditReasonCatalog.IsAllowed(SecurityAuditReasonCatalog.MfaFailure).Should().BeTrue();
        SecurityAuditReasonCatalog.IsAllowed(SecurityAuditReasonCatalog.SecondFactorRequired).Should().BeTrue();
        SecurityAuditReasonCatalog.IsAllowed(SecurityAuditReasonCatalog.AuthenticationProviderFailure).Should().BeTrue();
        SecurityAuditRedaction.Apply("password-value").Should().Be(SecurityAuditRedaction.Replacement);
        SecurityAuditRedaction.Apply("otp-value").Should().Be(SecurityAuditRedaction.Replacement);
    }

    [Fact]
    public void Catalog_declares_an_event_mapping_for_every_matrix_control()
    {
        HashSet<SecurityAuditControl> covered = SecurityAuditEventCatalog.Entries
            .Where(entry => entry.Applicable)
            .SelectMany(entry => entry.ControlItems)
            .ToHashSet();

        covered.Should().Contain(Enum.GetValues<SecurityAuditControl>());
        SecurityAuditEventCatalog.Entries
            .Single(entry => entry.EventName == SecurityAuditEventCatalog.SessionTerminatedEvent)
            .ControlItems.Should().Contain(SecurityAuditControl.AdministratorIdleTermination);
    }

    [Fact]
    public async Task Direct_review_lookups_do_not_record_allowed_or_denied_attempts()
    {
        DbContextOptions<AuditDbContext> options = CreateOptions();
        await using AuditDbContext context = CreateContext(options, "tenant-a");
        await context.Database.EnsureCreatedAsync();
        SecurityAuditEvent existing = CreateEvent("tenant-a", "existing", SecurityAuditEventCatalog.AuthenticationPasswordLoginEvent, DateTime.UtcNow);
        existing.Resource = "account/42";
        context.SecurityAuditEvents.Add(existing);
        await context.SaveChangesAsync();

        SecurityAuditQueryService authorized = new(
            context,
            new TestRequestContext("tenant-a", ["tenant-a:" + IdentityDefinitions.TenantOwner]));

        (await authorized.GetByResourceAsync("account/42", CancellationToken.None)).Should().ContainSingle();
        (await context.SecurityAuditEvents.CountAsync()).Should().Be(1);

        SecurityAuditQueryService denied = new(
            context,
            new TestRequestContext("tenant-a"));

        Func<Task> read = () => denied.GetByIdempotencyKeyAsync("existing", CancellationToken.None);
        await read.Should().ThrowAsync<UnauthorizedAccessException>();
        (await context.SecurityAuditEvents.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Review_filters_by_actor_and_sorts_by_event_type_with_capacity_warning()
    {
        DbContextOptions<AuditDbContext> options = CreateOptions();
        await using AuditDbContext context = CreateContext(options, "tenant-a");
        await context.Database.EnsureCreatedAsync();
        context.SecurityAuditEvents.AddRange(
            CreateEvent("tenant-a", "actor-b", SecurityAuditEventCatalog.AuthenticationPasswordLoginEvent, DateTime.UtcNow.AddMinutes(-2)),
            CreateEvent("tenant-a", "actor-a", SecurityAuditEventCatalog.AuthenticationPasswordChangeEvent, DateTime.UtcNow.AddMinutes(-1)));
        await context.SaveChangesAsync();

        SecurityAuditQueryService service = new(
            context,
            new TestRequestContext("tenant-a", ["tenant-a:" + IdentityDefinitions.TenantOwner]),
            new FixedStorageSizeReader(2048),
            Options.Create(new SecurityAuditOptions { CapacityWarningBytes = 1024 }));

        SecurityAuditPageReadDto page = await service.QueryAsync(
            new SecurityAuditQuery(Page: 1, PageSize: 10, ActorId: "actor-a", SortBy: SecurityAuditSortField.EventType, Descending: false),
            CancellationToken.None);

        page.Items.Should().ContainSingle();
        page.Items[0].EventName.Should().Be(SecurityAuditEventCatalog.AuthenticationPasswordChangeEvent);
        page.TotalCount.Should().Be(1);
        page.CapacityThresholdReached.Should().BeTrue();
    }

    [Theory]
    [InlineData(SecurityAuditSortField.Actor, false)]
    [InlineData(SecurityAuditSortField.Actor, true)]
    [InlineData(SecurityAuditSortField.ActorKind, false)]
    [InlineData(SecurityAuditSortField.ActorKind, true)]
    [InlineData(SecurityAuditSortField.OccurredAt, false)]
    [InlineData(SecurityAuditSortField.OccurredAt, true)]
    [InlineData(SecurityAuditSortField.SourceIpKind, false)]
    [InlineData(SecurityAuditSortField.SourceIpKind, true)]
    [InlineData(SecurityAuditSortField.EventType, false)]
    [InlineData(SecurityAuditSortField.EventType, true)]
    [InlineData(SecurityAuditSortField.SourceIpAddress, false)]
    [InlineData(SecurityAuditSortField.SourceIpAddress, true)]
    public async Task Review_sorts_every_supported_field_in_both_directions(SecurityAuditSortField sortBy, bool descending)
    {
        DbContextOptions<AuditDbContext> options = CreateOptions();
        await using AuditDbContext context = CreateContext(options, "tenant-a");
        await context.Database.EnsureCreatedAsync();
        DateTime first = new(2026, 9, 2, 7, 0, 0, DateTimeKind.Utc);
        SecurityAuditEvent actorB = CreateEvent("tenant-a", "actor-b", SecurityAuditEventCatalog.AuthenticationPasswordLoginEvent, first);
        actorB.ActorKind = SecurityAuditActorKind.System;
        actorB.SourceIpKind = SecurityAuditSourceKind.Client;
        actorB.SourceIpAddress = "203.0.113.2";
        SecurityAuditEvent actorA = CreateEvent("tenant-a", "actor-a", SecurityAuditEventCatalog.AuthenticationPasswordChangeEvent, first.AddMinutes(1));
        actorA.ActorKind = SecurityAuditActorKind.Anonymous;
        actorA.SourceIpKind = SecurityAuditSourceKind.Unknown;
        actorA.SourceIpAddress = "203.0.113.1";
        SecurityAuditEvent actorC = CreateEvent("tenant-a", "actor-c", SecurityAuditEventCatalog.EndpointMutationEvent, first.AddMinutes(2));
        actorC.ActorKind = SecurityAuditActorKind.User;
        actorC.SourceIpKind = SecurityAuditSourceKind.System;
        actorC.SourceIpAddress = "203.0.113.3";
        context.SecurityAuditEvents.AddRange(actorB, actorA, actorC);
        await context.SaveChangesAsync();

        SecurityAuditPageReadDto page = await new SecurityAuditQueryService(
            context,
            new TestRequestContext("tenant-a", ["tenant-a:" + IdentityDefinitions.TenantOwner]))
            .QueryAsync(new SecurityAuditQuery(PageSize: 10, SortBy: sortBy, Descending: descending), CancellationToken.None);

        string[] actual = sortBy switch
        {
            SecurityAuditSortField.Actor => page.Items.Select(item => item.ActorId!).ToArray(),
            SecurityAuditSortField.ActorKind => page.Items.Select(item => item.ActorKind!).ToArray(),
            SecurityAuditSortField.SourceIpKind => page.Items.Select(item => item.SourceIpKind!).ToArray(),
            SecurityAuditSortField.EventType => page.Items.Select(item => item.EventName).ToArray(),
            SecurityAuditSortField.SourceIpAddress => page.Items.Select(item => item.SourceIpAddress!).ToArray(),
            _ => page.Items.Select(item => item.OccurredAt).Select(value => value.ToString("O")).ToArray()
        };
        string[] expected = sortBy switch
        {
            SecurityAuditSortField.Actor => ["actor-a", "actor-b", "actor-c"],
            SecurityAuditSortField.ActorKind => [SecurityAuditActorKind.Anonymous, SecurityAuditActorKind.System, SecurityAuditActorKind.User],
            SecurityAuditSortField.SourceIpKind => [SecurityAuditSourceKind.Client, SecurityAuditSourceKind.System, SecurityAuditSourceKind.Unknown],
            SecurityAuditSortField.EventType => [
                SecurityAuditEventCatalog.AuthenticationPasswordChangeEvent,
                SecurityAuditEventCatalog.AuthenticationPasswordLoginEvent,
                SecurityAuditEventCatalog.EndpointMutationEvent
            ],
            SecurityAuditSortField.SourceIpAddress => ["203.0.113.1", "203.0.113.2", "203.0.113.3"],
            _ => [first.ToString("O"), first.AddMinutes(1).ToString("O"), first.AddMinutes(2).ToString("O")]
        };

        if (descending)
            expected = expected.Reverse().ToArray();
        actual.Should().Equal(expected);
    }

    [Fact]
    public async Task Review_applies_each_filter_pages_results_and_excludes_other_tenants()
    {
        DbContextOptions<AuditDbContext> options = CreateOptions();
        await using AuditDbContext context = CreateContext(options, "tenant-a");
        await context.Database.EnsureCreatedAsync();
        DateTime first = new(2026, 9, 2, 7, 0, 0, DateTimeKind.Utc);
        SecurityAuditEvent actorA = CreateEvent("tenant-a", "actor-a", SecurityAuditEventCatalog.AuthenticationPasswordLoginEvent, first);
        SecurityAuditEvent actorB = CreateEvent("tenant-a", "actor-b", SecurityAuditEventCatalog.AuthenticationPasswordChangeEvent, first.AddMinutes(1));
        SecurityAuditEvent function = CreateEvent("tenant-a", "actor-c", SecurityAuditEventCatalog.EndpointMutationEvent, first.AddMinutes(2));
        function.EventCategory = SecurityAuditEventCatalog.FunctionCategory;
        SecurityAuditEvent otherTenant = CreateEvent("tenant-b", "actor-b", SecurityAuditEventCatalog.AuthenticationPasswordLoginEvent, first.AddMinutes(3));
        context.SecurityAuditEvents.AddRange(actorA, actorB, function);
        await using (AuditDbContext otherTenantContext = CreateContext(options, "tenant-b"))
        {
            otherTenantContext.SecurityAuditEvents.Add(otherTenant);
            await otherTenantContext.SaveChangesAsync();
        }
        await context.SaveChangesAsync();

        SecurityAuditQueryService service = new(
            context,
            new TestRequestContext("tenant-a", ["tenant-a:" + IdentityDefinitions.TenantOwner]));

        (await service.QueryAsync(new SecurityAuditQuery(PageSize: 10, ActorId: "actor-b"), CancellationToken.None))
            .Items.Select(item => item.ActorId).Should().Equal("actor-b");
        (await service.QueryAsync(new SecurityAuditQuery(PageSize: 10, EventName: SecurityAuditEventCatalog.EndpointMutationEvent), CancellationToken.None))
            .Items.Select(item => item.EventName).Should().Equal(SecurityAuditEventCatalog.EndpointMutationEvent);
        (await service.QueryAsync(new SecurityAuditQuery(PageSize: 10, EventCategory: SecurityAuditEventCatalog.FunctionCategory), CancellationToken.None))
            .Items.Select(item => item.EventName).Should().Equal(SecurityAuditEventCatalog.EndpointMutationEvent);
        (await service.QueryAsync(new SecurityAuditQuery(PageSize: 10, FromUtc: first.AddMinutes(1), ToUtc: first.AddMinutes(1)), CancellationToken.None))
            .Items.Select(item => item.Id).Should().Equal(actorB.Id);

        SecurityAuditPageReadDto secondPage = await service.QueryAsync(
            new SecurityAuditQuery(Page: 2, PageSize: 1, SortBy: SecurityAuditSortField.OccurredAt, Descending: false),
            CancellationToken.None);
        secondPage.TotalCount.Should().Be(3);
        secondPage.Items.Select(item => item.Id).Should().Equal(actorB.Id);
    }

    [Fact]
    public async Task Platform_review_queries_all_tenants_and_separates_uncatalogued_rows()
    {
        DbContextOptions<AuditDbContext> options = CreateOptions();
        DateTime first = new(2026, 9, 2, 7, 0, 0, DateTimeKind.Utc);

        await using (AuditDbContext tenantA = CreateContext(options, "tenant-a"))
        {
            await tenantA.Database.EnsureCreatedAsync();
            tenantA.SecurityAuditEvents.Add(CreateEvent("tenant-a", "actor-a", SecurityAuditEventCatalog.AuthenticationPasswordLoginEvent, first));
            await tenantA.SaveChangesAsync();
        }

        await using (AuditDbContext tenantB = CreateContext(options, "tenant-b"))
        {
            SecurityAuditEvent failed = CreateEvent("tenant-b", "actor-b", SecurityAuditEventCatalog.AuthenticationPasswordLoginEvent, first.AddMinutes(1));
            failed.Outcome = SecurityAuditOutcome.Failed;
            SecurityAuditEvent legacy = CreateEvent("tenant-b", "legacy-user", "legacy.event", first.AddMinutes(2));
            legacy.EventCategory = null;
            legacy.Operation = "legacy.operation";
            tenantB.SecurityAuditEvents.AddRange(failed, legacy);
            await tenantB.SaveChangesAsync();
        }

        await using (AuditDbContext platform = CreateContext(options, SecurityAuditScopeCatalog.PlatformScopeId))
        {
            platform.SecurityAuditEvents.Add(new SecurityAuditEvent
            {
                Id = Guid.NewGuid(),
                EventName = SecurityAuditEventCatalog.AuditReviewEvent,
                EventCategory = SecurityAuditEventCatalog.ReviewCategory,
                Outcome = SecurityAuditOutcome.Succeeded,
                ActorKind = SecurityAuditActorKind.User,
                ActorId = "platform-reviewer",
                TenantId = SecurityAuditScopeCatalog.PlatformScopeId,
                Resource = SecurityAuditScopeCatalog.PlatformResource,
                Operation = SecurityAuditEventCatalog.AuditReviewOperation,
                OccurredAt = first.AddMinutes(3),
                CorrelationId = "platform-request",
                SourceBoundary = SecurityAuditEventCatalog.AuditReviewSourceBoundary,
                SourceIpKind = SecurityAuditSourceKind.Unknown,
                IdempotencyKey = "platform-request"
            });
            await platform.SaveChangesAsync();
        }

        await using AuditDbContext globalContext = CreateContext(options, SecurityAuditScopeCatalog.PlatformScopeId);
        SecurityAuditQueryService service = new(globalContext, new TestRequestContext(SecurityAuditScopeCatalog.PlatformScopeId, [IdentityDefinitions.PlatformAdmin]));

        SecurityAuditPageReadDto allScopes = await service.QueryPlatformAsync(
            new SecurityAuditPlatformQuery(new SecurityAuditQuery(PageSize: 10)),
            CancellationToken.None);
        allScopes.TotalCount.Should().Be(4);
        allScopes.Items.Select(item => item.TenantId).Should().Contain(["tenant-a", "tenant-b", SecurityAuditScopeCatalog.PlatformScopeId]);

        SecurityAuditPageReadDto tenantScopedReview = await service.QueryAsync(
            new SecurityAuditQuery(PageSize: 10),
            CancellationToken.None);
        tenantScopedReview.TotalCount.Should().Be(0);

        await using (AuditDbContext noTenantContext = CreateContext(options, null))
        {
            SecurityAuditPageReadDto noTenantReview = await new SecurityAuditQueryService(
                noTenantContext,
                new TestRequestContext(null, [IdentityDefinitions.PlatformAdmin]))
                .QueryPlatformAsync(new SecurityAuditPlatformQuery(new SecurityAuditQuery(PageSize: 10)), CancellationToken.None);
            noTenantReview.TotalCount.Should().Be(4);
        }

        SecurityAuditPageReadDto selectedTenant = await service.QueryPlatformAsync(
            new SecurityAuditPlatformQuery(new SecurityAuditQuery(PageSize: 10, Outcome: SecurityAuditOutcome.Failed), "tenant-b"),
            CancellationToken.None);
        selectedTenant.TotalCount.Should().Be(1);
        selectedTenant.Items.Single().ActorId.Should().Be("actor-b");

        Func<Task> platformTenantFilter = () => service.QueryPlatformAsync(
            new SecurityAuditPlatformQuery(new SecurityAuditQuery(PageSize: 10), SecurityAuditScopeCatalog.PlatformScopeId),
            CancellationToken.None);
        await platformTenantFilter.Should().ThrowAsync<ArgumentException>();

        SecurityAuditPageReadDto uncatalogued = await service.QueryPlatformAsync(
            new SecurityAuditPlatformQuery(new SecurityAuditQuery(PageSize: 10), UncataloguedOnly: true),
            CancellationToken.None);
        uncatalogued.TotalCount.Should().Be(1);
        uncatalogued.Items.Single().EventName.Should().Be("legacy.event");

        SecurityAuditPageReadDto exactUncatalogued = await service.QueryPlatformAsync(
            new SecurityAuditPlatformQuery(
                new SecurityAuditQuery(PageSize: 10, EventName: "legacy.event", Operation: "legacy.operation"),
                UncataloguedOnly: true),
            CancellationToken.None);
        exactUncatalogued.TotalCount.Should().Be(1);
        exactUncatalogued.Items.Single().EventName.Should().Be("legacy.event");

        Func<Task> categoryWithUncatalogued = () => service.QueryPlatformAsync(
            new SecurityAuditPlatformQuery(
                new SecurityAuditQuery(PageSize: 10, EventCategory: SecurityAuditEventCatalog.AuthenticationCategory),
                UncataloguedOnly: true),
            CancellationToken.None);
        await categoryWithUncatalogued.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Platform_review_lists_the_catalogued_audit_storage_threshold_and_security_failure_records()
    {
        DbContextOptions<AuditDbContext> options = CreateOptions();
        await using AuditDbContext context = CreateContext(options, SecurityAuditScopeCatalog.PlatformScopeId);
        await context.Database.EnsureCreatedAsync();
        DateTime occurredAt = new(2026, 10, 5, 8, 0, 0, DateTimeKind.Utc);

        SecurityAuditWriter writer = new(context);
        _ = await writer.WriteAsync(new SecurityAuditEvent
        {
            Id = Guid.NewGuid(),
            EventName = SecurityAuditEventCatalog.AuditStorageThresholdEvent,
            EventCategory = SecurityAuditEventCatalog.SecurityFailureCategory,
            Outcome = SecurityAuditOutcome.Succeeded,
            ActorKind = SecurityAuditActorKind.System,
            TenantId = SecurityAuditScopeCatalog.PlatformScopeId,
            Resource = SecurityAuditScopeCatalog.PlatformResource,
            Operation = SecurityAuditEventCatalog.AuditStorageThresholdOperation,
            OccurredAt = occurredAt,
            CorrelationId = "audit-storage-threshold",
            SourceBoundary = SecurityAuditEventCatalog.AuditStorageThresholdSourceBoundary,
            SourceIpKind = SecurityAuditSourceKind.System,
            RedactedReason = SecurityAuditReasonCatalog.AuditStorageThreshold(
                4096,
                1024,
                SecurityAuditReasonCatalog.NotificationChannelSucceeded,
                SecurityAuditReasonCatalog.NotificationChannelSucceeded),
            IdempotencyKey = "audit-storage-threshold"
        }, CancellationToken.None);
        _ = await writer.WriteAsync(new SecurityAuditEvent
        {
            Id = Guid.NewGuid(),
            EventName = SecurityAuditEventCatalog.SecurityFailureEvent,
            EventCategory = SecurityAuditEventCatalog.SecurityFailureCategory,
            Outcome = SecurityAuditOutcome.Failed,
            ActorKind = SecurityAuditActorKind.System,
            TenantId = SecurityAuditScopeCatalog.PlatformScopeId,
            Resource = SecurityAuditScopeCatalog.PlatformResource,
            Operation = SecurityAuditEventCatalog.SecurityFailureOperation,
            OccurredAt = occurredAt.AddMinutes(1),
            CorrelationId = "audit-storage-check-failure",
            SourceBoundary = SecurityAuditEventCatalog.AuditStorageThresholdSourceBoundary,
            SourceIpKind = SecurityAuditSourceKind.System,
            RedactedReason = SecurityAuditReasonCatalog.AuditStoreUnavailable,
            IdempotencyKey = "audit-storage-check-failure"
        }, CancellationToken.None);

        SecurityAuditQueryService service = new(
            context,
            new TestRequestContext(SecurityAuditScopeCatalog.PlatformScopeId, [IdentityDefinitions.PlatformAdmin]));

        SecurityAuditPageReadDto platform = await service.QueryPlatformAsync(
            new SecurityAuditPlatformQuery(new SecurityAuditQuery(PageSize: 10)),
            CancellationToken.None);
        platform.TotalCount.Should().Be(2);
        platform.Items.Select(item => item.EventName).Should().BeEquivalentTo(
            [SecurityAuditEventCatalog.AuditStorageThresholdEvent, SecurityAuditEventCatalog.SecurityFailureEvent]);
        platform.Items.Select(item => item.RedactedReason).Should().NotContain(SecurityAuditRedaction.Replacement);

        SecurityAuditPageReadDto uncatalogued = await service.QueryPlatformAsync(
            new SecurityAuditPlatformQuery(new SecurityAuditQuery(PageSize: 10), UncataloguedOnly: true),
            CancellationToken.None);
        uncatalogued.TotalCount.Should().Be(0);

        SecurityAuditPageReadDto failureCategory = await service.QueryPlatformAsync(
            new SecurityAuditPlatformQuery(
                new SecurityAuditQuery(PageSize: 10, EventCategory: SecurityAuditEventCatalog.SecurityFailureCategory)),
            CancellationToken.None);
        failureCategory.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task Platform_export_rejects_more_than_the_bounded_row_limit()
    {
        DbContextOptions<AuditDbContext> options = CreateOptions();
        await using AuditDbContext context = CreateContext(options, SecurityAuditScopeCatalog.PlatformScopeId);
        await context.Database.EnsureCreatedAsync();
        List<SecurityAuditEvent> events = Enumerable.Range(0, SecurityAuditExportPolicy.ProbeRows)
            .Select(index => CreateEvent(SecurityAuditScopeCatalog.PlatformScopeId, $"actor-{index}", SecurityAuditEventCatalog.AuthenticationPasswordLoginEvent, DateTime.UtcNow.AddSeconds(-index)))
            .ToList();
        context.SecurityAuditEvents.AddRange(events);
        await context.SaveChangesAsync();

        SecurityAuditQueryService service = new(context, new TestRequestContext(SecurityAuditScopeCatalog.PlatformScopeId, [IdentityDefinitions.PlatformAdmin]));

        Func<Task> export = () => service.ExportPlatformAsync(
            new SecurityAuditPlatformQuery(new SecurityAuditQuery(PageSize: 10)),
            CancellationToken.None);

        await export.Should().ThrowAsync<SecurityAuditExportLimitException>();
    }

    [Fact]
    public async Task Platform_and_tenant_review_queries_do_not_change_audit_state()
    {
        DbContextOptions<AuditDbContext> options = CreateOptions();
        await using AuditDbContext context = CreateContext(options, SecurityAuditScopeCatalog.PlatformScopeId);
        await context.Database.EnsureCreatedAsync();

        SecurityAuditEvent existingReview = new()
        {
            Id = Guid.NewGuid(),
            EventName = SecurityAuditEventCatalog.AuditReviewEvent,
            EventCategory = SecurityAuditEventCatalog.ReviewCategory,
            Outcome = SecurityAuditOutcome.Succeeded,
            ActorKind = SecurityAuditActorKind.User,
            ActorId = "platform-reviewer",
            TenantId = SecurityAuditScopeCatalog.PlatformScopeId,
            Resource = SecurityAuditScopeCatalog.PlatformResource,
            Operation = SecurityAuditEventCatalog.AuditReviewOperation,
            OccurredAt = new DateTime(2026, 9, 2, 7, 0, 0, DateTimeKind.Utc),
            CorrelationId = "historical-review",
            SourceBoundary = SecurityAuditEventCatalog.AuditReviewSourceBoundary,
            SourceIpKind = SecurityAuditSourceKind.Unknown,
            IdempotencyKey = "historical-review"
        };
        context.SecurityAuditEvents.Add(existingReview);
        await context.SaveChangesAsync();

        SecurityAuditQueryService service = new(
            context,
            new TestRequestContext(SecurityAuditScopeCatalog.PlatformScopeId, [IdentityDefinitions.PlatformAdmin]));

        SecurityAuditPageReadDto platformPage = await service.QueryPlatformAsync(
            new SecurityAuditPlatformQuery(new SecurityAuditQuery(PageSize: 10)),
            CancellationToken.None);
        platformPage.TotalCount.Should().Be(1);

        await service.GetPlatformCatalogAsync(CancellationToken.None);
        (await service.ExportPlatformAsync(
            new SecurityAuditPlatformQuery(new SecurityAuditQuery(PageSize: 10)),
            CancellationToken.None)).Should().ContainSingle();

        (await service.QueryAsync(new SecurityAuditQuery(PageSize: 10), CancellationToken.None))
            .TotalCount.Should().Be(0);
        (await service.GetByResourceAsync(SecurityAuditScopeCatalog.PlatformResource, CancellationToken.None))
            .Should().BeEmpty();
        (await service.GetByIdempotencyKeyAsync("historical-review", CancellationToken.None))
            .Should().BeNull();

        (await context.SecurityAuditEvents.CountAsync()).Should().Be(1);
        SecurityAuditEvent persistedReview = await context.SecurityAuditEvents.SingleAsync();
        persistedReview.Id.Should().Be(existingReview.Id);
        persistedReview.Outcome.Should().Be(SecurityAuditOutcome.Succeeded);
        persistedReview.IdempotencyKey.Should().Be("historical-review");
    }

    [Fact]
    public async Task Denied_platform_review_does_not_append_an_audit_event()
    {
        DbContextOptions<AuditDbContext> options = CreateOptions();
        await using AuditDbContext context = CreateContext(options, SecurityAuditScopeCatalog.PlatformScopeId);
        await context.Database.EnsureCreatedAsync();
        context.SecurityAuditEvents.Add(CreateEvent(
            SecurityAuditScopeCatalog.PlatformScopeId,
            "existing-event",
            SecurityAuditEventCatalog.AuthenticationPasswordLoginEvent,
            new DateTime(2026, 9, 2, 7, 0, 0, DateTimeKind.Utc)));
        await context.SaveChangesAsync();

        SecurityAuditQueryService deniedService = new(context, new TestRequestContext(null));

        Func<Task> deniedRead = () => deniedService.QueryPlatformAsync(
            new SecurityAuditPlatformQuery(new SecurityAuditQuery(PageSize: 10)),
            CancellationToken.None);

        await deniedRead.Should().ThrowAsync<UnauthorizedAccessException>();
        (await context.SecurityAuditEvents.CountAsync()).Should().Be(1);
    }

    [Fact]
    public void Csv_export_contains_only_review_safe_fields_and_escapes_formula_cells()
    {
        string csv = System.Text.Encoding.UTF8.GetString(SecurityAuditCsvFormatter.Format(
        [
            new SecurityAuditEventReadDto
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                EventName = "=unsafe",
                EventCategory = SecurityAuditEventCatalog.AuthenticationCategory,
                Outcome = SecurityAuditOutcome.Succeeded,
                ActorKind = SecurityAuditActorKind.User,
                ActorId = "actor,one",
                TenantId = "tenant-a",
                Operation = SecurityAuditEventCatalog.PasswordLoginOperation,
                OccurredAt = new DateTime(2026, 9, 2, 7, 0, 0, DateTimeKind.Utc),
                CorrelationId = "request-1",
                SourceBoundary = "tests",
                SourceIpKind = SecurityAuditSourceKind.Unknown,
                RedactedReason = "line one\nline two",
                ResourceName = "=referral.pdf",
                ResourceNameHash = new string('a', SecurityAuditEvent.MaximumResourceNameHashLength),
                ResourceExtension = "pdf",
                ResourceSize = 2048,
                Changes = [new SecurityAuditChange(SecurityAuditChangeFields.Email, "old@example.test", "new@example.test")]
            }
        ]));

        csv.Should().Contain("\"'=unsafe\"");
        csv.Should().Contain("\"actor,one\"");
        csv.Should().Contain("\"line one\nline two\"");
        csv.Should().StartWith("\"id\"").And.Contain("\"resourceNameHash\",\"resourceExtension\",\"resourceSize\",");
        csv.Should().Contain($"\"{new string('a', SecurityAuditEvent.MaximumResourceNameHashLength)}\",\"pdf\",\"2048\",");
        csv.Should().Contain("\"changes\",\"resourceName\"\r\n").And.Contain("previousValue");
        csv.Should().EndWith(",\"'=referral.pdf\"\r\n");
        csv.Should().NotContain("idempotency");
        csv.Should().NotContain("payload");
    }

    [Fact]
    public void Csv_changes_are_json_encoded_and_distinguish_missing_values_from_empty_values()
    {
        string changes = SecurityAuditCsvFormatter.FormatChanges(
        [
            new SecurityAuditChange(SecurityAuditChangeFields.Email, "old@example.test", "new@example.test"),
            new SecurityAuditChange(SecurityAuditChangeFields.FirstName, null, string.Empty),
            new SecurityAuditChange(SecurityAuditChangeFields.LastName, "old; victim -> value", "new\\path")
        ]);

        changes.Should().Contain("\"field\":\"email\"");
        changes.Should().Contain("\"previousValue\":\"old@example.test\"");
        changes.Should().Contain($"\"previousValue\":\"{SecurityAuditEventCatalog.ChangeValueNone}\"");
        changes.Should().Contain("\"newValue\":\"\"");
        changes.Should().Contain("old; victim -\\u003E value");
        changes.Should().Contain("new\\\\path");
        changes.Should().NotContain("email: old@example.test -> new@example.test");
    }

    [Fact]
    public async Task Tenant_review_never_exposes_field_changes()
    {
        DbContextOptions<AuditDbContext> options = CreateOptions();
        await using AuditDbContext context = CreateContext(options, "tenant-a");
        await context.Database.EnsureCreatedAsync();
        SecurityAuditEvent edited = CreateEvent(
            "tenant-a",
            "actor-a",
            SecurityAuditEventCatalog.UserEditEvent,
            new DateTime(2026, 9, 2, 7, 0, 0, DateTimeKind.Utc));
        edited.EventCategory = SecurityAuditEventCatalog.ManagementCategory;
        edited.Operation = SecurityAuditEventCatalog.UserEditOperation;
        edited.Resource = "account/42";
        edited.Changes = [new SecurityAuditChange(SecurityAuditChangeFields.Email, "old@example.test", "new@example.test")];
        context.SecurityAuditEvents.Add(edited);
        await context.SaveChangesAsync();

        SecurityAuditQueryService tenantOwner = new(
            context,
            new TestRequestContext("tenant-a", ["tenant-a:" + IdentityDefinitions.TenantOwner]));
        (await tenantOwner.QueryAsync(new SecurityAuditQuery(PageSize: 10), CancellationToken.None))
            .Items.Single().Changes.Should().BeNull();
        (await tenantOwner.GetByResourceAsync("account/42", CancellationToken.None))
            .Single().Changes.Should().BeNull();

        SecurityAuditQueryService platform = new(
            context,
            new TestRequestContext("tenant-a", [IdentityDefinitions.PlatformAdmin]));
        (await platform.QueryPlatformAsync(
            new SecurityAuditPlatformQuery(new SecurityAuditQuery(PageSize: 10)),
            CancellationToken.None)).Items.Single().Changes
            .Should().Equal(new SecurityAuditChange(SecurityAuditChangeFields.Email, "old@example.test", "new@example.test"));
    }

    [Fact]
    public async Task Review_query_requires_the_existing_selected_tenant_authorization()
    {
        DbContextOptions<AuditDbContext> options = CreateOptions();
        await using AuditDbContext context = CreateContext(options, "tenant-a");
        await context.Database.EnsureCreatedAsync();

        Func<Task> read = () => new SecurityAuditQueryService(context, new TestRequestContext("tenant-a"))
            .QueryAsync(new SecurityAuditQuery(), CancellationToken.None);

        await read.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Store_failure_logs_one_redacted_error_and_recovery_does_not_invent_the_lost_event()
    {
        DbContextOptions<AuditDbContext> options = CreateOptions();
        AuditDbContext unavailable = CreateContext(options, "tenant-a");
        await unavailable.Database.EnsureCreatedAsync();
        await unavailable.DisposeAsync();
        List<string> logs = [];
        SecurityAuditEvent lost = CreateEvent("tenant-a", "lost-event", SecurityAuditEventCatalog.AuthenticationPasswordLoginEvent, DateTime.UtcNow);
        lost.RedactedReason = "patient payload must never appear in host logs";

        Func<Task> write = () => new SecurityAuditWriter(
            unavailable,
            logger: new CapturingLogger<SecurityAuditWriter>(logs))
            .WriteAsync(lost, CancellationToken.None);

        await write.Should().ThrowAsync<SecurityAuditStoreException>();
        logs.Should().ContainSingle();
        logs.Single().Should().Contain(SecurityAuditEventCatalog.WriteFailureCode)
            .And.Contain("Cause=" + typeof(ObjectDisposedException).FullName)
            .And.Contain("Site=")
            .And.NotContain("patient payload")
            .And.NotContain("Cannot access a disposed");

        await using AuditDbContext recovered = CreateContext(options, "tenant-a");
        SecurityAuditEvent restored = CreateEvent("tenant-a", "recovered-event", SecurityAuditEventCatalog.AuthenticationPasswordLoginEvent, DateTime.UtcNow);
        (await new SecurityAuditWriter(recovered).WriteAsync(restored, CancellationToken.None))
            .Should().Be(SecurityAuditWriteResult.Recorded);
        (await recovered.SecurityAuditEvents.CountAsync()).Should().Be(1);
        (await recovered.SecurityAuditEvents.AnyAsync(item => item.IdempotencyKey == "lost-event")).Should().BeFalse();
    }

    [Fact]
    public async Task Legacy_rows_remain_readable_without_fabricated_metadata()
    {
        DbContextOptions<AuditDbContext> options = CreateOptions();
        await using AuditDbContext context = CreateContext(options, "tenant-a");
        await context.Database.EnsureCreatedAsync();
        context.SecurityAuditEvents.Add(new SecurityAuditEvent
        {
            Id = Guid.NewGuid(),
            EventName = "legacy.event",
            Outcome = SecurityAuditOutcome.Succeeded,
            ActorId = "legacy-user",
            TenantId = "tenant-a",
            Operation = "legacy.operation",
            OccurredAt = DateTime.UtcNow,
            CorrelationId = "legacy-request",
            SourceBoundary = "legacy",
            IdempotencyKey = "legacy-request"
        });
        await context.SaveChangesAsync();

        SecurityAuditEventReadDto? result = await new SecurityAuditQueryService(
            context,
            new TestRequestContext("tenant-a", ["tenant-a:" + IdentityDefinitions.TenantOwner]))
            .GetByIdempotencyKeyAsync("legacy-request", CancellationToken.None);

        result.Should().NotBeNull();
        result!.EventCategory.Should().BeNull();
        result.ActorKind.Should().BeNull();
        result.SourceIpKind.Should().BeNull();
        result.SourceIpAddress.Should().BeNull();
    }

    [Fact]
    public async Task Review_returns_dependency_unavailable_when_the_store_cannot_be_read()
    {
        DbContextOptions<AuditDbContext> options = CreateOptions();
        AuditDbContext context = CreateContext(options, "tenant-a");
        await context.Database.EnsureCreatedAsync();
        SecurityAuditQueryService service = new(
            context,
            new TestRequestContext("tenant-a", ["tenant-a:" + IdentityDefinitions.TenantOwner]));
        await context.DisposeAsync();

        Func<Task> read = () => service.QueryAsync(new SecurityAuditQuery(), CancellationToken.None);

        await read.Should().ThrowAsync<SecurityAuditStoreException>();
    }

    private static SecurityAuditEvent CreateEvent(string tenantId, string actorId, string eventName, DateTime occurredAt) => new()
    {
        Id = Guid.NewGuid(),
        EventName = eventName,
        EventCategory = SecurityAuditEventCatalog.AuthenticationCategory,
        Outcome = SecurityAuditOutcome.Succeeded,
        ActorKind = SecurityAuditActorKind.User,
        ActorId = actorId,
        TenantId = tenantId,
        Operation = eventName == SecurityAuditEventCatalog.AuthenticationPasswordChangeEvent
            ? SecurityAuditEventCatalog.PasswordChangeOperation
            : SecurityAuditEventCatalog.PasswordLoginOperation,
        OccurredAt = DateTime.SpecifyKind(occurredAt, DateTimeKind.Utc),
        CorrelationId = Guid.NewGuid().ToString("N"),
        SourceBoundary = "tests",
        SourceIpKind = SecurityAuditSourceKind.Unknown,
        IdempotencyKey = Guid.NewGuid().ToString("N")
    };

    private static AuditDbContext CreateContext(DbContextOptions<AuditDbContext> options, string? tenantId) =>
        new(options, new TestRequestContext(tenantId));

    private static DbContextOptions<AuditDbContext> CreateOptions() =>
        new DbContextOptionsBuilder<AuditDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

    private sealed class FixedStorageSizeReader(long size) : IAuditStorageSizeReader
    {
        public Task<long> GetStorageSizeBytesAsync(CancellationToken cancellationToken) => Task.FromResult(size);
    }

    private sealed class CapturingLogger<T>(List<string> messages) : Microsoft.Extensions.Logging.ILogger<T>
    {
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => true;

        public void Log<TState>(
            Microsoft.Extensions.Logging.LogLevel logLevel,
            Microsoft.Extensions.Logging.EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
            => messages.Add(formatter(state, exception));

        private sealed class NullScope : IDisposable
        {
            public static NullScope Instance { get; } = new();
            public void Dispose() { }
        }
    }

    private sealed class TestClientIpAddressAccessor(string? address) : IClientIpAddressAccessor
    {
        public string? ClientIpAddress { get; } = address;
    }

    private sealed class TestRequestContext(string? tenantId, string[]? roles = null, string? requestId = null) : IRequestContext
    {
        public string Authority => "test";
        public string? ClientId => null;
        public string? OwnerId => null;
        public string? UserId => "reviewer";
        public string? TenantId => tenantId;
        public string? ResourceId => null;
        public string? TraceId => "trace";
        public string? RequestId => requestId ?? "request";
        public string? AdditionalInfo => null;
        public ClaimsPrincipal? User => new(new ClaimsIdentity((roles ?? []).Select(role => new Claim(ClaimTypes.Role, role)), "test"));
        public Request GetRequest() => new() { Authority = Authority, TenantId = TenantId };
    }
}
