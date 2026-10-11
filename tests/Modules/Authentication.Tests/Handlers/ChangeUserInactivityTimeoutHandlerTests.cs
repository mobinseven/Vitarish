using System.Security.Claims;
using FluentAssertions;
using Moq;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Application.Handlers;
using Vitarish.Authentication.Application.Security;
using Vitarish.Authentication.Domain.Security;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Requests.Domain.Entities;

namespace Vitarish.Authentication.Tests.Handlers;

public sealed class ChangeUserInactivityTimeoutHandlerTests
{
    [Fact]
    public async Task User_directory_failure_leaves_no_pending_audit_for_retry()
    {
        Mock<IAuthenticationSessionRegistry> sessions = new();
        Mock<ISecurityAuditWriter> auditWriter = new();
        Mock<IAuthenticationSecurityPolicyProvider> policyProvider = new();
        Mock<IAuthenticationUserDirectory> directory = new();
        directory.Setup(item => item.FindByIdAsync("user-1", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("directory offline"));
        ChangeUserInactivityTimeoutHandler handler = new(sessions.Object, auditWriter.Object, new TestRequestContext(), policyProvider.Object, directory.Object);

        Func<Task> act = () => handler.Handle("user-1", 10, 0, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        auditWriter.Verify(item => item.WriteAsync(It.IsAny<SecurityAuditEvent>(), It.IsAny<CancellationToken>()), Times.Never);
        sessions.Verify(item => item.SetUserInactivityTimeoutAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Policy_read_failure_leaves_no_pending_audit_for_retry()
    {
        Mock<IAuthenticationSessionRegistry> sessions = new();
        Mock<ISecurityAuditWriter> auditWriter = new();
        Mock<IAuthenticationSecurityPolicyProvider> policyProvider = new();
        policyProvider.Setup(item => item.GetAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("policy store offline"));
        Mock<IAuthenticationUserDirectory> directory = new();
        directory.Setup(item => item.FindByIdAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Descriptor("user-1"));
        ChangeUserInactivityTimeoutHandler handler = new(sessions.Object, auditWriter.Object, new TestRequestContext(), policyProvider.Object, directory.Object);

        Func<Task> act = () => handler.Handle("user-1", 10, 0, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        auditWriter.Verify(item => item.WriteAsync(It.IsAny<SecurityAuditEvent>(), It.IsAny<CancellationToken>()), Times.Never);
        sessions.Verify(item => item.SetUserInactivityTimeoutAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Inactivity_state_read_failure_leaves_no_pending_audit_for_retry()
    {
        Mock<IAuthenticationSessionRegistry> sessions = new();
        sessions.Setup(item => item.GetUserInactivityTimeoutStateAsync("user-1", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("session store offline"));
        Mock<ISecurityAuditWriter> auditWriter = new();
        Mock<IAuthenticationSecurityPolicyProvider> policyProvider = new();
        policyProvider.Setup(item => item.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(SecurityAccessPolicy.Default);
        Mock<IAuthenticationUserDirectory> directory = new();
        directory.Setup(item => item.FindByIdAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Descriptor("user-1"));
        ChangeUserInactivityTimeoutHandler handler = new(sessions.Object, auditWriter.Object, new TestRequestContext(), policyProvider.Object, directory.Object);

        Func<Task> act = () => handler.Handle("user-1", 10, 0, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        auditWriter.Verify(item => item.WriteAsync(It.IsAny<SecurityAuditEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Missing_user_is_denied_and_audited()
    {
        Mock<IAuthenticationSessionRegistry> sessions = new();
        Mock<IAuthenticationSecurityPolicyProvider> policyProvider = new();
        policyProvider.Setup(item => item.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(SecurityAccessPolicy.Default);
        Mock<IAuthenticationUserDirectory> directory = new();
        directory.Setup(item => item.FindByIdAsync("missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((AuthenticationUserDescriptor?)null);
        RecordingAuditWriter audit = new();
        ChangeUserInactivityTimeoutHandler handler = new(sessions.Object, audit, new TestRequestContext(), policyProvider.Object, directory.Object);

        Func<Task> act = () => handler.Handle("missing", 10, 0, CancellationToken.None);

        await act.Should().ThrowAsync<AuthenticationUserNotFoundException>();
        audit.Events.Should().ContainSingle(item =>
            item.Outcome == SecurityAuditOutcome.Denied
            && item.RedactedReason == AuthenticationSecurityAuditEvents.UserPolicyTargetNotFoundReason);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(129601)]
    public async Task Override_outside_one_minute_through_ninety_days_is_rejected_before_any_effect(int minutes)
    {
        Mock<IAuthenticationSessionRegistry> sessions = new();
        Mock<ISecurityAuditWriter> auditWriter = new();
        ChangeUserInactivityTimeoutHandler handler = new(sessions.Object, auditWriter.Object, new TestRequestContext(), Mock.Of<IAuthenticationSecurityPolicyProvider>(), Mock.Of<IAuthenticationUserDirectory>());

        Func<Task> act = () => handler.Handle("user-1", minutes, 0, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
        auditWriter.Verify(item => item.WriteAsync(It.IsAny<SecurityAuditEvent>(), It.IsAny<CancellationToken>()), Times.Never);
        sessions.Verify(item => item.SetUserInactivityTimeoutAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Successful_change_persists_the_timeout_and_completes_audit()
    {
        Mock<IAuthenticationSessionRegistry> sessions = new();
        sessions.Setup(item => item.GetUserInactivityTimeoutStateAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, (int?)null, 0L));
        sessions.Setup(item => item.RevokeSessionsExpiredByUserPolicyAsync("user-1", It.IsAny<CancellationToken>(), It.IsAny<int?>()))
            .Returns(Task.CompletedTask);
        sessions.Setup(item => item.SetUserInactivityTimeoutAsync("user-1", 10, 0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1L);
        Mock<IAuthenticationSecurityPolicyProvider> policyProvider = new();
        policyProvider.Setup(item => item.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(SecurityAccessPolicy.Default);
        Mock<IAuthenticationUserDirectory> directory = new();
        directory.Setup(item => item.FindByIdAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Descriptor("user-1"));
        RecordingAuditWriter audit = new();
        ChangeUserInactivityTimeoutHandler handler = new(sessions.Object, audit, new TestRequestContext(), policyProvider.Object, directory.Object);

        long version = await handler.Handle("user-1", 10, 0, CancellationToken.None);

        version.Should().Be(1);
        audit.Events.Select(item => item.Outcome).Should().Contain(SecurityAuditOutcome.Succeeded);
        sessions.Verify(item => item.SetUserInactivityTimeoutAsync("user-1", 10, 0, It.IsAny<CancellationToken>()), Times.Once);
    }

    private static AuthenticationUserDescriptor Descriptor(string userId) =>
        new(userId, "operator", null, null, null, null, null, false);

    private sealed class RecordingAuditWriter : ISecurityAuditWriter, ISecurityAuditOutcomeUpdater
    {
        public List<SecurityAuditEvent> Events { get; } = [];

        public Task<string?> GetOutcomeAsync(string tenantId, string idempotencyKey, CancellationToken cancellationToken) =>
            Task.FromResult(Events.SingleOrDefault(item => item.IdempotencyKey == idempotencyKey)?.Outcome);

        public Task<SecurityAuditWriteResult> WriteAsync(SecurityAuditEvent auditEvent, CancellationToken cancellationToken)
        {
            if (Events.Any(item => item.IdempotencyKey == auditEvent.IdempotencyKey))
                return Task.FromResult(SecurityAuditWriteResult.AlreadyRecorded);
            Events.Add(auditEvent);
            return Task.FromResult(SecurityAuditWriteResult.Recorded);
        }

        public Task<SecurityAuditWriteResult> UpdateAsync(string tenantId, string idempotencyKey, string outcome, string? reason, CancellationToken cancellationToken)
        {
            SecurityAuditEvent? pending = Events.SingleOrDefault(item => item.IdempotencyKey == idempotencyKey);
            if (pending is null || pending.Outcome != SecurityAuditOutcome.Pending)
                return Task.FromResult(SecurityAuditWriteResult.AlreadyRecorded);
            pending.Outcome = outcome;
            pending.RedactedReason = reason;
            return Task.FromResult(SecurityAuditWriteResult.Recorded);
        }
    }

    private sealed class TestRequestContext : IRequestContext
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
        public ClaimsPrincipal? User => new(new ClaimsIdentity([new Claim(ClaimTypes.Role, IdentityDefinitions.PlatformAdmin)], "test"));
        public Request GetRequest() => new() { RequestId = RequestId!, Authority = Authority, TenantId = TenantId, UserId = UserId };
    }
}
