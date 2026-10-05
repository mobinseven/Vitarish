using System.Security.Claims;
using FluentAssertions;
using Moq;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Application.Handlers;
using Vitarish.Authentication.Application.Security;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Requests.Domain.Entities;

namespace Vitarish.Authentication.Tests.Handlers;

public sealed class RevokeUserSessionsHandlerTests
{
    [Fact]
    public async Task Administrator_revokes_every_session_and_completes_the_audit()
    {
        Mock<IAuthenticationUserDirectory> directory = new();
        directory.Setup(item => item.FindByIdAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Descriptor("user-1"));
        Mock<IAuthenticationSessionRegistry> sessions = new();
        sessions.Setup(item => item.RevokeActiveSessionsAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);
        RecordingAuditWriter audit = new();
        RevokeUserSessionsHandler handler = new(directory.Object, sessions.Object, audit, new TestRequestContext([IdentityDefinitions.PlatformAdmin]));

        int ended = await handler.Handle("user-1", CancellationToken.None);

        ended.Should().Be(3);
        sessions.Verify(item => item.RevokeActiveSessionsAsync("user-1", It.IsAny<CancellationToken>()), Times.Once);
        audit.Events.Select(item => item.Outcome).Should().ContainInOrder(SecurityAuditOutcome.Pending, SecurityAuditOutcome.Succeeded);
        audit.Events.Should().OnlyContain(item =>
            item.EventName == AuthenticationSecurityAuditEvents.SessionTerminated
            && item.Operation == AuthenticationSecurityAuditEvents.SessionTerminatedOperation
            && item.ActorId == "admin-1"
            && item.SubjectId == "user-1"
            && item.RedactedReason == AuthenticationSecurityAuditEvents.SessionAdministrativeRevocation);
    }

    [Fact]
    public async Task Revoking_with_nothing_active_succeeds_with_zero_and_still_completes_the_audit()
    {
        Mock<IAuthenticationUserDirectory> directory = new();
        directory.Setup(item => item.FindByIdAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Descriptor("user-1"));
        Mock<IAuthenticationSessionRegistry> sessions = new();
        sessions.Setup(item => item.RevokeActiveSessionsAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        RecordingAuditWriter audit = new();
        RevokeUserSessionsHandler handler = new(directory.Object, sessions.Object, audit, new TestRequestContext([IdentityDefinitions.PlatformAdmin]));

        int ended = await handler.Handle("user-1", CancellationToken.None);

        ended.Should().Be(0);
        audit.Events.Select(item => item.Outcome).Should().ContainInOrder(SecurityAuditOutcome.Pending, SecurityAuditOutcome.Succeeded);
        audit.Events[^1].RedactedReason.Should().Be(AuthenticationSecurityAuditEvents.SessionAdministrativeRevocation);
    }

    [Fact]
    public async Task Unknown_user_is_denied_and_audited_without_touching_sessions()
    {
        Mock<IAuthenticationUserDirectory> directory = new();
        directory.Setup(item => item.FindByIdAsync("missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((AuthenticationUserDescriptor?)null);
        Mock<IAuthenticationSessionRegistry> sessions = new();
        RecordingAuditWriter audit = new();
        RevokeUserSessionsHandler handler = new(directory.Object, sessions.Object, audit, new TestRequestContext([IdentityDefinitions.PlatformAdmin]));

        Func<Task> act = () => handler.Handle("missing", CancellationToken.None);

        await act.Should().ThrowAsync<AuthenticationUserNotFoundException>();
        sessions.Verify(item => item.RevokeActiveSessionsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        audit.Events.Select(item => item.Outcome).Should().ContainInOrder(SecurityAuditOutcome.Pending, SecurityAuditOutcome.Denied);
        audit.Events[^1].RedactedReason.Should().Be(AuthenticationSecurityAuditEvents.UserPolicyTargetNotFoundReason);
    }

    [Fact]
    public async Task Non_administrator_is_rejected_and_audited_without_touching_sessions()
    {
        Mock<IAuthenticationUserDirectory> directory = new();
        Mock<IAuthenticationSessionRegistry> sessions = new();
        RecordingAuditWriter audit = new();
        RevokeUserSessionsHandler handler = new(directory.Object, sessions.Object, audit, new TestRequestContext([]));

        Func<Task> act = () => handler.Handle("user-1", CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        directory.VerifyNoOtherCalls();
        sessions.VerifyNoOtherCalls();
        audit.Events.Should().ContainSingle(item =>
            item.Outcome == SecurityAuditOutcome.Denied
            && item.ActorId == "admin-1"
            && item.SubjectId == "user-1"
            && item.RedactedReason == AuthenticationSecurityAuditEvents.Unauthorized);
    }

    [Fact]
    public async Task A_replayed_operation_returns_zero_without_repeating_the_registry_mutation()
    {
        Mock<IAuthenticationUserDirectory> directory = new();
        directory.Setup(item => item.FindByIdAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Descriptor("user-1"));
        Mock<IAuthenticationSessionRegistry> sessions = new();
        sessions.Setup(item => item.RevokeActiveSessionsAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);
        RecordingAuditWriter audit = new();
        RevokeUserSessionsHandler handler = new(directory.Object, sessions.Object, audit, new TestRequestContext([IdentityDefinitions.PlatformAdmin]));

        int first = await handler.Handle("user-1", CancellationToken.None);
        int replay = await handler.Handle("user-1", CancellationToken.None);

        first.Should().Be(2);
        replay.Should().Be(0);
        sessions.Verify(item => item.RevokeActiveSessionsAsync("user-1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task A_retried_request_for_an_unknown_user_still_returns_not_found()
    {
        Mock<IAuthenticationUserDirectory> directory = new();
        directory.Setup(item => item.FindByIdAsync("missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((AuthenticationUserDescriptor?)null);
        Mock<IAuthenticationSessionRegistry> sessions = new();
        RecordingAuditWriter audit = new();
        RevokeUserSessionsHandler handler = new(directory.Object, sessions.Object, audit, new TestRequestContext([IdentityDefinitions.PlatformAdmin]));

        await FluentActions.Awaiting(() => handler.Handle("missing", CancellationToken.None)).Should().ThrowAsync<AuthenticationUserNotFoundException>();
        await FluentActions.Awaiting(() => handler.Handle("missing", CancellationToken.None)).Should().ThrowAsync<AuthenticationUserNotFoundException>();

        sessions.Verify(item => item.RevokeActiveSessionsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        audit.Events.Should().NotContain(item => item.Outcome == SecurityAuditOutcome.Succeeded);
    }

    [Fact]
    public async Task A_failure_after_the_sessions_ended_is_never_recorded_as_denied()
    {
        Mock<IAuthenticationUserDirectory> directory = new();
        directory.Setup(item => item.FindByIdAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Descriptor("user-1"));
        Mock<IAuthenticationSessionRegistry> sessions = new();
        sessions.Setup(item => item.RevokeActiveSessionsAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);
        RecordingAuditWriter audit = new() { FailSucceededUpdates = true };
        RevokeUserSessionsHandler handler = new(directory.Object, sessions.Object, audit, new TestRequestContext([IdentityDefinitions.PlatformAdmin]));

        Func<Task> act = () => handler.Handle("user-1", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        audit.Events.Should().NotContain(item => item.Outcome == SecurityAuditOutcome.Denied);
    }

    private static AuthenticationUserDescriptor Descriptor(string userId) =>
        new(userId, "operator", null, null, null, null, null, false);

    private sealed class RecordingAuditWriter : ISecurityAuditWriter, ISecurityAuditOutcomeUpdater
    {
        public List<SecurityAuditEvent> Events { get; } = [];

        public bool FailSucceededUpdates { get; init; }

        public Task<string?> GetOutcomeAsync(string tenantId, string idempotencyKey, CancellationToken cancellationToken) =>
            Task.FromResult(Events.LastOrDefault(item => item.IdempotencyKey == idempotencyKey)?.Outcome);

        public Task<SecurityAuditWriteResult> WriteAsync(SecurityAuditEvent auditEvent, CancellationToken cancellationToken)
        {
            if (Events.Any(item => item.IdempotencyKey == auditEvent.IdempotencyKey))
                return Task.FromResult(SecurityAuditWriteResult.AlreadyRecorded);
            Events.Add(auditEvent);
            return Task.FromResult(SecurityAuditWriteResult.Recorded);
        }

        public Task<SecurityAuditWriteResult> UpdateAsync(string tenantId, string idempotencyKey, string outcome, string? reason, CancellationToken cancellationToken)
        {
            if (FailSucceededUpdates && outcome == SecurityAuditOutcome.Succeeded)
                throw new InvalidOperationException("Simulated failure after the sessions were ended.");
            SecurityAuditEvent? pending = Events.LastOrDefault(item => item.IdempotencyKey == idempotencyKey && item.Outcome == SecurityAuditOutcome.Pending);
            if (pending is null)
                return Task.FromResult(SecurityAuditWriteResult.AlreadyRecorded);
            Events.Add(new SecurityAuditEvent
            {
                EventName = pending.EventName,
                Outcome = outcome,
                ActorId = pending.ActorId,
                SubjectId = pending.SubjectId,
                TenantId = pending.TenantId,
                Resource = pending.Resource,
                Operation = pending.Operation,
                OccurredAt = pending.OccurredAt,
                CorrelationId = pending.CorrelationId,
                SourceBoundary = pending.SourceBoundary,
                RedactedReason = reason,
                IdempotencyKey = idempotencyKey
            });
            return Task.FromResult(SecurityAuditWriteResult.Recorded);
        }
    }

    private sealed class TestRequestContext(string[] roles) : IRequestContext
    {
        public string Authority => "test";
        public string? ClientId => null;
        public string? OwnerId => "owner-1";
        public string? UserId => "admin-1";
        public string? TenantId => "tenant-a";
        public string? ResourceId => "authentication/security-access-policy";
        public string? TraceId => "trace-1";
        public string? RequestId => "request-1";
        public string? AdditionalInfo => null;
        public ClaimsPrincipal? User => roles.Length == 0
            ? new ClaimsPrincipal(new ClaimsIdentity())
            : new ClaimsPrincipal(new ClaimsIdentity([.. roles.Select(role => new Claim(ClaimTypes.Role, role))], "test"));
        public Request GetRequest() => new() { RequestId = RequestId!, Authority = Authority, TenantId = TenantId, UserId = UserId };
    }
}
