using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Application.Security;
using Vitarish.Authentication.Domain.Security;
using Vitarish.Authentication.Domain.Sessions;
using Vitarish.Authentication.Infrastructure;
using Vitarish.Requests.Application.Abstractions;

namespace Authentication.Tests.Infrastructure;

public sealed class AuthenticationSessionRegistryTests
{
    [Fact]
    public async Task Establishing_a_second_session_with_default_policy_terminates_the_first()
    {
        TestTimeProvider clock = new(new DateTimeOffset(2026, 8, 30, 10, 0, 0, TimeSpan.Zero));
        (AuthenticationSessionRegistry registry, AuthenticationDbContext db, _) = CreateRegistry(SecurityAccessPolicy.Default, clock);

        AuthenticationSessionEstablishmentResult first = await registry.EstablishAsync("user-1", CancellationToken.None);
        AuthenticationSessionEstablishmentResult second = await registry.EstablishAsync("user-1", CancellationToken.None);

        first.Succeeded.Should().BeTrue();
        second.Succeeded.Should().BeTrue();
        (await db.AuthenticationSessions.SingleAsync(session => session.Id == Guid.Parse(first.SessionId!))).TerminationReason
            .Should().Be(AuthenticationSessionTerminationReason.ReplacedByNewSession);
        (await db.AuthenticationSessions.CountAsync(session => session.UserId == "user-1" && session.TerminatedAt == null)).Should().Be(1);
    }

    [Fact]
    public async Task Functionally_required_parallel_sessions_are_limited_and_notify_the_prior_session()
    {
        SecurityAccessPolicy policy = SecurityAccessPolicy.Default with
        {
            MaximumConcurrentSessions = 2,
            ConcurrentSessionsFunctionallyRequired = true
        };
        (AuthenticationSessionRegistry registry, AuthenticationDbContext db, _) = CreateRegistry(policy, new TestTimeProvider(DateTimeOffset.UtcNow));

        AuthenticationSessionEstablishmentResult first = await registry.EstablishAsync("user-1", CancellationToken.None);
        AuthenticationSessionEstablishmentResult second = await registry.EstablishAsync("user-1", CancellationToken.None);
        AuthenticationSessionEstablishmentResult third = await registry.EstablishAsync("user-1", CancellationToken.None);

        first.Succeeded.Should().BeTrue();
        second.Succeeded.Should().BeTrue();
        third.Succeeded.Should().BeFalse();
        third.Failure!.Reason.Should().Be(AuthenticationFailureReason.TooManyAttempts);
        (await db.AuthenticationSessionNotices.CountAsync(notice => notice.SessionId == Guid.Parse(first.SessionId!))).Should().Be(1);
        (await db.AuthenticationSessionNotices.CountAsync(notice => notice.SessionId == Guid.Parse(second.SessionId!))).Should().Be(0);
    }

    [Fact]
    public async Task Failed_attempts_are_carried_into_the_next_successful_access_history_record()
    {
        TestTimeProvider clock = new(new DateTimeOffset(2026, 8, 30, 10, 0, 0, TimeSpan.Zero));
        (AuthenticationSessionRegistry registry, AuthenticationDbContext db, _) = CreateRegistry(SecurityAccessPolicy.Default, clock);

        await registry.RecordFailedAttemptAsync("user-1", CancellationToken.None);
        (await db.AuthenticationAccessHistories.SingleAsync()).CurrentFailedAttempts.Should().Be(1);
        (await registry.GetAccessHistoryAsync("user-1", "tenant-1", CancellationToken.None))!.CurrentFailedAttempts.Should().Be(1);
        AuthenticationSessionEstablishmentResult first = await registry.EstablishAsync("user-1", CancellationToken.None);
        clock.UtcNow = clock.UtcNow.AddMinutes(1);
        AuthenticationSessionEstablishmentResult second = await registry.EstablishAsync("user-1", CancellationToken.None);

        first.AccessHistory!.FailedAttemptsSincePreviousSuccessful.Should().Be(1);
        second.AccessHistory!.PreviousSuccessfulAt.Should().Be(first.AccessHistory.LastSuccessfulAt);
        second.AccessHistory!.FailedAttemptsSincePreviousSuccessful.Should().Be(0);
    }

    [Fact]
    public async Task Access_history_records_the_ip_address_of_each_successful_and_unsuccessful_attempt()
    {
        string currentAddress = "203.0.113.9";
        DbContextOptions<AuthenticationDbContext> options = new DbContextOptionsBuilder<AuthenticationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        AuthenticationDbContext db = new(options);
        Mock<IAuthenticationSecurityPolicyProvider> policyProvider = new();
        policyProvider.Setup(provider => provider.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(SecurityAccessPolicy.Default);
        Mock<IClientIpAddressAccessor> clientIp = new();
        clientIp.SetupGet(accessor => accessor.ClientIpAddress).Returns(() => currentAddress);
        Mock<IRequestContext> requestContext = new();
        requestContext.SetupGet(context => context.TenantId).Returns("tenant-1");
        requestContext.SetupGet(context => context.RequestId).Returns("request-1");
        TestTimeProvider clock = new(new DateTimeOffset(2026, 8, 30, 10, 0, 0, TimeSpan.Zero));
        AuthenticationSessionRegistry registry = new(db, policyProvider.Object, clientIp.Object, requestContext.Object, new RecordingAuditWriter(), clock);

        await registry.RecordFailedAttemptAsync("user-1", CancellationToken.None);
        currentAddress = "198.51.100.7";
        _ = await registry.EstablishAsync("user-1", CancellationToken.None);
        clock.UtcNow = clock.UtcNow.AddMinutes(1);
        currentAddress = "198.51.100.8";
        AuthenticationSessionEstablishmentResult second = await registry.EstablishAsync("user-1", CancellationToken.None);

        second.AccessHistory!.LastUnsuccessfulIpAddress.Should().Be("203.0.113.9");
        second.AccessHistory.PreviousSuccessfulIpAddress.Should().Be("198.51.100.7");
        second.AccessHistory.LastSuccessfulIpAddress.Should().Be("198.51.100.8");
        AuthenticationAccessHistory stored = (await registry.GetAccessHistoryAsync("user-1", "tenant-1", CancellationToken.None))!;
        stored.LastSuccessfulIpAddress.Should().Be("198.51.100.8");
        stored.LastUnsuccessfulIpAddress.Should().Be("203.0.113.9");
    }

    [Fact]
    public async Task User_inactivity_timeout_rejects_a_stale_expected_version()
    {
        (AuthenticationSessionRegistry registry, AuthenticationDbContext db, _) = CreateRegistry(SecurityAccessPolicy.Default, TimeProvider.System);

        await registry.SetUserInactivityTimeoutAsync("user-1", 10, 0, CancellationToken.None);

        Func<Task> act = () => registry.SetUserInactivityTimeoutAsync("user-1", 15, 0, CancellationToken.None);
        await act.Should().ThrowAsync<AuthenticationUserSessionPolicyConcurrencyException>();

        AuthenticationUserSessionPolicyRecord record = await db.AuthenticationUserSessionPolicies.SingleAsync();
        record.InactivityTimeoutMinutes.Should().Be(10);
        record.Version.Should().Be(1);
    }

    [Fact]
    public async Task Audit_failure_preserves_session_replacement_and_successful_access_history()
    {
        TestTimeProvider clock = new(new DateTimeOffset(2026, 8, 30, 10, 0, 0, TimeSpan.Zero));
        (AuthenticationSessionRegistry registry, AuthenticationDbContext db, RecordingAuditWriter audit) = CreateRegistry(SecurityAccessPolicy.Default, clock);

        AuthenticationSessionEstablishmentResult first = await registry.EstablishAsync("user-1", CancellationToken.None);
        DateTime? firstLastSuccessfulAt = (await db.AuthenticationAccessHistories.SingleAsync()).LastSuccessfulAt;
        audit.FailSuccessfulSessionEstablishment = true;
        clock.UtcNow = clock.UtcNow.AddMinutes(1);

        AuthenticationSessionEstablishmentResult second = await registry.EstablishAsync("user-1", CancellationToken.None);
        second.Succeeded.Should().BeTrue();

        AuthenticationSession restored = await db.AuthenticationSessions.SingleAsync(session => session.Id == Guid.Parse(first.SessionId!));
        restored.TerminatedAt.Should().NotBeNull();
        restored.TerminationReason.Should().Be(AuthenticationSessionTerminationReason.ReplacedByNewSession);
        (await db.AuthenticationSessions.CountAsync(session => session.UserId == "user-1" && session.TerminatedAt == null)).Should().Be(1);
        (await db.AuthenticationAccessHistories.SingleAsync()).LastSuccessfulAt.Should().NotBe(firstLastSuccessfulAt);
        (await db.AuthenticationAccessHistories.SingleAsync()).CurrentFailedAttempts.Should().Be(0);
    }

    [Fact]
    public async Task Audit_failure_on_first_establishment_preserves_new_history_and_session()
    {
        (AuthenticationSessionRegistry registry, AuthenticationDbContext db, RecordingAuditWriter audit) = CreateRegistry(SecurityAccessPolicy.Default, TimeProvider.System);
        audit.FailSuccessfulSessionEstablishment = true;

        AuthenticationSessionEstablishmentResult result = await registry.EstablishAsync("user-1", CancellationToken.None);
        result.Succeeded.Should().BeTrue();

        (await db.AuthenticationSessions.CountAsync()).Should().Be(1);
        (await db.AuthenticationAccessHistories.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Establishment_returns_the_effective_per_user_inactivity_timeout()
    {
        TestTimeProvider clock = new(new DateTimeOffset(2026, 8, 30, 10, 0, 0, TimeSpan.Zero));
        (AuthenticationSessionRegistry registry, AuthenticationDbContext db, _) = CreateRegistry(SecurityAccessPolicy.Default, clock);
        _ = db.AuthenticationUserSessionPolicies.Add(new AuthenticationUserSessionPolicyRecord
        {
            UserId = "user-1",
            InactivityTimeoutMinutes = 1,
            Version = 1
        });
        await db.SaveChangesAsync();

        AuthenticationSessionEstablishmentResult result = await registry.EstablishAsync("user-1", CancellationToken.None);

        result.InactivityTimeoutMinutes.Should().Be(1);
    }

    [Fact]
    public async Task Establishment_accepts_an_ip_inside_a_configured_cidr()
    {
        SecurityAccessPolicy policy = SecurityAccessPolicy.Default with
        {
            AllowedSessionStartCidrs = new HashSet<string>(StringComparer.Ordinal) { "10.0.0.0/24" }
        };
        (AuthenticationSessionRegistry registry, _, _) = CreateRegistry(policy, TimeProvider.System, "10.0.0.7");

        (await registry.EstablishAsync("user-1", CancellationToken.None)).Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Policy_change_revokes_sessions_outside_the_new_cidr()
    {
        (AuthenticationSessionRegistry registry, AuthenticationDbContext db, _) = CreateRegistry(SecurityAccessPolicy.Default, TimeProvider.System, "127.0.0.1");
        _ = await registry.EstablishAsync("user-1", CancellationToken.None);

        SecurityAccessPolicy updated = SecurityAccessPolicy.Default with
        {
            AllowedSessionStartCidrs = new HashSet<string>(StringComparer.Ordinal) { "10.0.0.0/24" }
        };
        await registry.SavePolicyAndRevokeInvalidatedSessionsAsync(updated, 0, CancellationToken.None);

        (await db.AuthenticationSessions.SingleAsync()).TerminationReason.Should().Be(AuthenticationSessionTerminationReason.AdministrativeRevocation);
    }

    [Fact]
    public async Task Policy_save_work_does_not_grow_with_previously_terminated_sessions()
    {
        OutcomeTrackingAuditWriter audit = new();
        (AuthenticationSessionRegistry registry, AuthenticationDbContext db) = CreateRegistry(SecurityAccessPolicy.Default, TimeProvider.System, audit);
        DateTime earlier = DateTime.UtcNow.AddDays(-30);
        for (int index = 0; index < 50; index++)
        {
            _ = db.AuthenticationSessions.Add(new AuthenticationSession
            {
                Id = Guid.NewGuid(),
                UserId = $"former-{index}",
                TenantId = "tenant-1",
                CreatedAt = earlier,
                LastActivityAt = earlier,
                ClientIpAddress = "127.0.0.1",
                TerminatedAt = earlier,
                TerminationReason = AuthenticationSessionTerminationReason.AdministrativeRevocation
            });
        }
        _ = await db.SaveChangesAsync();
        _ = await registry.EstablishAsync("user-1", CancellationToken.None);
        int readsBeforeSave = audit.OutcomeReads;

        await registry.SavePolicyAndRevokeInvalidatedSessionsAsync(
            SecurityAccessPolicy.Default with { AllowedSessionStartCidrs = new HashSet<string>(StringComparer.Ordinal) { "10.0.0.0/24" } },
            0,
            CancellationToken.None);

        audit.OutcomeReads.Should().Be(readsBeforeSave);
        (await db.AuthenticationSessions.AsNoTracking().SingleAsync(session => session.UserId == "user-1")).TerminationReason
            .Should().Be(AuthenticationSessionTerminationReason.AdministrativeRevocation);
    }

    [Fact]
    public async Task Policy_change_is_persisted_through_the_registry_context_with_the_revocation()
    {
        (AuthenticationSessionRegistry registry, AuthenticationDbContext db, _) = CreateRegistry(SecurityAccessPolicy.Default, TimeProvider.System, "127.0.0.1");
        _ = await registry.EstablishAsync("user-1", CancellationToken.None);
        SecurityAccessPolicy updated = SecurityAccessPolicy.Default with
        {
            AllowedSessionStartCidrs = new HashSet<string>(StringComparer.Ordinal) { "10.0.0.0/24" }
        };

        await registry.SavePolicyAndRevokeInvalidatedSessionsAsync(updated, 0, CancellationToken.None);

        (await db.SecurityAccessPolicies.AsNoTracking().SingleAsync()).AllowedSessionStartIpAddresses.Should().Be("10.0.0.0/24");
        (await db.AuthenticationSessions.SingleAsync()).TerminatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task User_timeout_change_revokes_only_sessions_expired_under_the_committed_timeout()
    {
        SecurityAccessPolicy policy = SecurityAccessPolicy.Default with
        {
            MaximumConcurrentSessions = 2,
            ConcurrentSessionsFunctionallyRequired = true
        };
        TestTimeProvider clock = new(new DateTimeOffset(2026, 8, 30, 10, 0, 0, TimeSpan.Zero));
        (AuthenticationSessionRegistry registry, AuthenticationDbContext db, _) = CreateRegistry(policy, clock);

        await registry.SetUserInactivityTimeoutAsync("user-1", 1, 0, CancellationToken.None);
        AuthenticationSession first = new()
        {
            Id = Guid.NewGuid(),
            UserId = "user-1",
            TenantId = "tenant-1",
            CreatedAt = clock.UtcNow.UtcDateTime,
            LastActivityAt = clock.UtcNow.UtcDateTime,
            ClientIpAddress = "127.0.0.1"
        };
        AuthenticationSession second = new()
        {
            Id = Guid.NewGuid(),
            UserId = "user-1",
            TenantId = "tenant-1",
            CreatedAt = clock.UtcNow.UtcDateTime.AddMinutes(44),
            LastActivityAt = clock.UtcNow.UtcDateTime.AddMinutes(44),
            ClientIpAddress = "127.0.0.1"
        };
        db.AuthenticationSessions.AddRange(first, second);
        await db.SaveChangesAsync();
        clock.UtcNow = clock.UtcNow.AddMinutes(44.5);

        await registry.RevokeSessionsExpiredByUserPolicyAsync("user-1", CancellationToken.None);

        (await db.AuthenticationSessions.SingleAsync(session => session.Id == first.Id)).TerminationReason
            .Should().Be(AuthenticationSessionTerminationReason.AdministrativeRevocation);
        (await db.AuthenticationSessions.SingleAsync(session => session.Id == second.Id)).TerminatedAt.Should().BeNull();
    }

    [Fact]
    public async Task Active_session_query_returns_false_after_explicit_revocation()
    {
        (AuthenticationSessionRegistry registry, _, _) = CreateRegistry(SecurityAccessPolicy.Default, TimeProvider.System);
        AuthenticationSessionEstablishmentResult result = await registry.EstablishAsync("user-1", CancellationToken.None);

        (await registry.HasActiveSessionAsync("user-1", CancellationToken.None)).Should().BeTrue();

        await registry.RevokeAsync(result.SessionId!, AuthenticationSessionTerminationReason.UserLogout, CancellationToken.None);

        (await registry.HasActiveSessionAsync("user-1", CancellationToken.None)).Should().BeFalse();
    }

    [Fact]
    public async Task Active_session_query_returns_false_after_inactivity_timeout()
    {
        TestTimeProvider clock = new(new DateTimeOffset(2026, 8, 30, 10, 0, 0, TimeSpan.Zero));
        SecurityAccessPolicy policy = SecurityAccessPolicy.Default with { DefaultInactivityTimeoutMinutes = 1 };
        (AuthenticationSessionRegistry registry, _, _) = CreateRegistry(policy, clock);
        _ = await registry.EstablishAsync("user-1", CancellationToken.None);

        clock.UtcNow = clock.UtcNow.AddMinutes(1);

        (await registry.HasActiveSessionAsync("user-1", CancellationToken.None)).Should().BeFalse();
    }

    [Fact]
    public async Task Administrative_revocation_ends_every_active_session_of_the_target_user_with_audit()
    {
        TestTimeProvider clock = new(new DateTimeOffset(2026, 8, 30, 10, 0, 0, TimeSpan.Zero));
        (AuthenticationSessionRegistry registry, AuthenticationDbContext db, RecordingAuditWriter audit) = CreateRegistry(SecurityAccessPolicy.Default, clock);
        AuthenticationSession first = CreateSession("user-1", clock, TimeSpan.Zero);
        AuthenticationSession second = CreateSession("user-1", clock, TimeSpan.FromMinutes(1));
        AuthenticationSession otherUser = CreateSession("user-2", clock, TimeSpan.Zero);
        db.AuthenticationSessions.AddRange(first, second, otherUser);
        await db.SaveChangesAsync();

        int ended = await registry.RevokeActiveSessionsAsync("user-1", CancellationToken.None);

        ended.Should().Be(2);
        AuthenticationSession storedFirst = await db.AuthenticationSessions.AsNoTracking().SingleAsync(session => session.Id == first.Id);
        AuthenticationSession storedSecond = await db.AuthenticationSessions.AsNoTracking().SingleAsync(session => session.Id == second.Id);
        storedFirst.TerminationReason.Should().Be(AuthenticationSessionTerminationReason.AdministrativeRevocation);
        storedSecond.TerminationReason.Should().Be(AuthenticationSessionTerminationReason.AdministrativeRevocation);
        storedFirst.TerminatedAt.Should().Be(clock.UtcNow.UtcDateTime);
        storedSecond.TerminatedAt.Should().Be(clock.UtcNow.UtcDateTime);
        (await db.AuthenticationSessions.AsNoTracking().SingleAsync(session => session.Id == otherUser.Id)).TerminatedAt.Should().BeNull();
        audit.Events.Should().OnlyContain(item =>
            item.EventName == AuthenticationSecurityAuditEvents.SessionTerminated
            && item.Operation == AuthenticationSecurityAuditEvents.SessionTerminatedOperation
            && item.ActorId == "admin-1"
            && item.SubjectId == "user-1"
            && item.RedactedReason == AuthenticationSecurityAuditEvents.SessionAdministrativeRevocation);
        audit.Events.Where(item => item.Outcome == SecurityAuditOutcome.Pending).Should().HaveCount(2);
        audit.Events.Where(item => item.Outcome == SecurityAuditOutcome.Succeeded).Should().HaveCount(2);
    }

    [Fact]
    public async Task Administrative_revocation_is_idempotent_and_does_not_audit_an_empty_revocation()
    {
        TestTimeProvider clock = new(new DateTimeOffset(2026, 8, 30, 10, 0, 0, TimeSpan.Zero));
        (AuthenticationSessionRegistry registry, AuthenticationDbContext db, RecordingAuditWriter audit) = CreateRegistry(SecurityAccessPolicy.Default, clock);
        db.AuthenticationSessions.Add(CreateSession("user-1", clock, TimeSpan.Zero));
        await db.SaveChangesAsync();

        int first = await registry.RevokeActiveSessionsAsync("user-1", CancellationToken.None);
        int second = await registry.RevokeActiveSessionsAsync("user-1", CancellationToken.None);
        int unknownUser = await registry.RevokeActiveSessionsAsync("missing-user", CancellationToken.None);

        first.Should().Be(1);
        second.Should().Be(0);
        unknownUser.Should().Be(0);
        audit.Events.Should().HaveCount(2);
        (await db.AuthenticationSessions.AsNoTracking().SingleAsync()).TerminationReason.Should().Be(AuthenticationSessionTerminationReason.AdministrativeRevocation);
    }

    [Fact]
    public async Task Administrative_revocation_retry_closes_audits_left_open_after_the_sessions_were_ended()
    {
        TestTimeProvider clock = new(new DateTimeOffset(2026, 8, 30, 10, 0, 0, TimeSpan.Zero));
        OutcomeTrackingAuditWriter audit = new() { FailSucceededUpdates = true };
        (AuthenticationSessionRegistry registry, AuthenticationDbContext db) = CreateRegistry(SecurityAccessPolicy.Default, clock, audit);
        AuthenticationSession first = CreateSession("user-1", clock, TimeSpan.Zero);
        AuthenticationSession second = CreateSession("user-1", clock, TimeSpan.FromMinutes(1));
        db.AuthenticationSessions.AddRange(first, second);
        await db.SaveChangesAsync();

        await FluentActions.Awaiting(() => registry.RevokeActiveSessionsAsync("user-1", CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>();
        (await db.AuthenticationSessions.AsNoTracking().CountAsync(session => session.TerminatedAt != null)).Should().Be(2);

        audit.FailSucceededUpdates = false;
        int retried = await registry.RevokeActiveSessionsAsync("user-1", CancellationToken.None);

        retried.Should().Be(0);
        audit.OutcomeOf(TerminationKey(first)).Should().Be(SecurityAuditOutcome.Succeeded);
        audit.OutcomeOf(TerminationKey(second)).Should().Be(SecurityAuditOutcome.Succeeded);
    }

    [Fact]
    public async Task Administrative_revocation_retry_resumes_its_own_pending_audits_for_sessions_still_active()
    {
        TestTimeProvider clock = new(new DateTimeOffset(2026, 8, 30, 10, 0, 0, TimeSpan.Zero));
        OutcomeTrackingAuditWriter audit = new();
        (AuthenticationSessionRegistry registry, AuthenticationDbContext db) = CreateRegistry(SecurityAccessPolicy.Default, clock, audit);
        AuthenticationSession session = CreateSession("user-1", clock, TimeSpan.Zero);
        db.AuthenticationSessions.Add(session);
        await db.SaveChangesAsync();
        audit.Seed(TerminationKey(session), SecurityAuditOutcome.Pending);

        int ended = await registry.RevokeActiveSessionsAsync("user-1", CancellationToken.None);

        ended.Should().Be(1);
        (await db.AuthenticationSessions.AsNoTracking().SingleAsync()).TerminationReason.Should().Be(AuthenticationSessionTerminationReason.AdministrativeRevocation);
        audit.OutcomeOf(TerminationKey(session)).Should().Be(SecurityAuditOutcome.Succeeded);
    }

    private static string TerminationKey(AuthenticationSession session) =>
        $"{AuthenticationSecurityAuditEvents.SessionTerminated}:request-1:{session.Id:N}";

    private static (AuthenticationSessionRegistry Registry, AuthenticationDbContext Db, RecordingAuditWriter Audit) CreateRegistry(SecurityAccessPolicy policy, TimeProvider clock, string address = "127.0.0.1")
    {
        RecordingAuditWriter audit = new();
        (AuthenticationSessionRegistry registry, AuthenticationDbContext db) = CreateRegistry(policy, clock, audit, address);
        return (registry, db, audit);
    }

    private static (AuthenticationSessionRegistry Registry, AuthenticationDbContext Db) CreateRegistry(SecurityAccessPolicy policy, TimeProvider clock, ISecurityAuditWriter audit, string address = "127.0.0.1")
    {
        DbContextOptions<AuthenticationDbContext> options = new DbContextOptionsBuilder<AuthenticationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        AuthenticationDbContext db = new(options);
        Mock<IAuthenticationSecurityPolicyProvider> policyProvider = new();
        policyProvider.Setup(provider => provider.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(policy);
        Mock<IClientIpAddressAccessor> clientIp = new();
        clientIp.SetupGet(accessor => accessor.ClientIpAddress).Returns(address);
        Mock<IRequestContext> requestContext = new();
        requestContext.SetupGet(context => context.TenantId).Returns("tenant-1");
        requestContext.SetupGet(context => context.UserId).Returns("admin-1");
        requestContext.SetupGet(context => context.RequestId).Returns("request-1");
        requestContext.SetupGet(context => context.TraceId).Returns("trace-1");
        return (new AuthenticationSessionRegistry(db, policyProvider.Object, clientIp.Object, requestContext.Object, audit, clock), db);
    }

    private static AuthenticationSession CreateSession(string userId, TestTimeProvider clock, TimeSpan offset) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        TenantId = "tenant-1",
        CreatedAt = clock.UtcNow.UtcDateTime.Add(offset),
        LastActivityAt = clock.UtcNow.UtcDateTime.Add(offset),
        ClientIpAddress = "127.0.0.1"
    };

    private sealed class RecordingAuditWriter : ISecurityAuditWriter
    {
        public List<SecurityAuditEvent> Events { get; } = [];
        public bool FailSuccessfulSessionEstablishment { get; set; }

        public Task<SecurityAuditWriteResult> WriteAsync(SecurityAuditEvent auditEvent, CancellationToken cancellationToken)
        {
            if (FailSuccessfulSessionEstablishment &&
                auditEvent.EventName == AuthenticationSecurityAuditEvents.SessionEstablished &&
                auditEvent.Outcome == SecurityAuditOutcome.Succeeded)
                throw new SecurityAuditStoreException("offline", new InvalidOperationException());
            Events.Add(auditEvent);
            return Task.FromResult(SecurityAuditWriteResult.Recorded);
        }
    }

    /// <summary>Idempotent writer with outcome updates, like the production audit store.</summary>
    private sealed class OutcomeTrackingAuditWriter : ISecurityAuditWriter, ISecurityAuditOutcomeUpdater
    {
        private readonly Dictionary<string, string> outcomes = new(StringComparer.Ordinal);

        public bool FailSucceededUpdates { get; set; }

        public void Seed(string idempotencyKey, string outcome) => outcomes[idempotencyKey] = outcome;

        public string? OutcomeOf(string idempotencyKey) => outcomes.GetValueOrDefault(idempotencyKey);

        public Task<SecurityAuditWriteResult> WriteAsync(SecurityAuditEvent auditEvent, CancellationToken cancellationToken)
        {
            if (!outcomes.TryAdd(auditEvent.IdempotencyKey, auditEvent.Outcome))
                return Task.FromResult(SecurityAuditWriteResult.AlreadyRecorded);
            return Task.FromResult(SecurityAuditWriteResult.Recorded);
        }

        public int OutcomeReads { get; private set; }

        public Task<string?> GetOutcomeAsync(string tenantId, string idempotencyKey, CancellationToken cancellationToken)
        {
            OutcomeReads++;
            return Task.FromResult(OutcomeOf(idempotencyKey));
        }

        public Task<SecurityAuditWriteResult> UpdateAsync(string tenantId, string idempotencyKey, string outcome, string? reason, CancellationToken cancellationToken)
        {
            if (FailSucceededUpdates && outcome == SecurityAuditOutcome.Succeeded)
                throw new InvalidOperationException("Simulated failure after the sessions were ended.");
            if (OutcomeOf(idempotencyKey) != SecurityAuditOutcome.Pending)
                return Task.FromResult(SecurityAuditWriteResult.AlreadyRecorded);
            outcomes[idempotencyKey] = outcome;
            return Task.FromResult(SecurityAuditWriteResult.Recorded);
        }
    }

    private sealed class TestTimeProvider(DateTimeOffset initialUtcNow) : TimeProvider
    {
        public DateTimeOffset UtcNow { get; set; } = initialUtcNow;

        public override DateTimeOffset GetUtcNow() => UtcNow;
    }
}
