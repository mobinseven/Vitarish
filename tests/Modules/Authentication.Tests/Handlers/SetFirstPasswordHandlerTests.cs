using System.Text.Json;
using FluentAssertions;
using Moq;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Application.Commands;
using Vitarish.Authentication.Application.Handlers;
using Vitarish.Authentication.Application.Results;
using Vitarish.Authentication.Application.Security;
using Vitarish.Authentication.Application.Services;
using Vitarish.Authentication.Domain.Security;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Requests.Domain.Entities;

namespace Vitarish.Authentication.Tests.Handlers;

public sealed class SetFirstPasswordHandlerTests
{
    private const string UserId = "account-42";
    private const string NewPassword = "StrongTest7!Passphrase";
    private static readonly DateTime Now = new(2026, 11, 6, 8, 30, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Existing_password_rejects_first_password_and_does_not_repeat_the_mutation()
    {
        Mock<IUserAccountUpdater> updater = new();
        updater.Setup(item => item.UserHasPasswordAsync(UserId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        Mock<IAuthenticationSessionRegistry> sessions = RecentSession();
        List<SecurityAuditEvent> events = [];
        SetFirstPasswordHandler handler = CreateHandler(updater, sessions, events);

        AuthenticationResult<bool> result = await handler.Handle(new(UserId, NewPassword), CancellationToken.None);

        result.Should().BeOfType<AuthenticationResult<bool>.Failure>()
            .Which.Error.Reason.Should().Be(AuthenticationFailureReason.Conflict);
        updater.Verify(item => item.AddFirstPasswordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        events.Should().ContainSingle(item => item.Outcome == SecurityAuditOutcome.Denied);
        AuditText(events).Should().NotContain(NewPassword);
    }

    [Fact]
    public async Task Missing_or_stale_authentication_is_rejected_before_password_policy_or_mutation()
    {
        Mock<IUserAccountUpdater> updater = new();
        Mock<IAuthenticationSessionRegistry> sessions = SessionAt(Now - RecentAuthenticationPolicy.MaximumAge - TimeSpan.FromTicks(1));
        Mock<IAuthenticationSecurityPolicyProvider> policies = new();
        List<SecurityAuditEvent> events = [];
        SetFirstPasswordHandler handler = CreateHandler(updater, sessions, events, policies);

        AuthenticationResult<bool> result = await handler.Handle(new(UserId, NewPassword), CancellationToken.None);

        result.Should().BeOfType<AuthenticationResult<bool>.Failure>()
            .Which.Error.Reason.Should().Be(AuthenticationFailureReason.Unauthorized);
        policies.Verify(item => item.GetAsync(It.IsAny<CancellationToken>()), Times.Never);
        updater.VerifyNoOtherCalls();
        events.Should().ContainSingle(item => item.Outcome == SecurityAuditOutcome.Denied);
    }

    [Fact]
    public async Task Missing_authentication_timestamp_is_rejected_before_password_policy_or_mutation()
    {
        Mock<IUserAccountUpdater> updater = new();
        Mock<IAuthenticationSessionRegistry> sessions = SessionAt(null);
        Mock<IAuthenticationSecurityPolicyProvider> policies = new();
        List<SecurityAuditEvent> events = [];
        SetFirstPasswordHandler handler = CreateHandler(updater, sessions, events, policies);

        AuthenticationResult<bool> result = await handler.Handle(new(UserId, NewPassword), CancellationToken.None);

        result.Should().BeOfType<AuthenticationResult<bool>.Failure>()
            .Which.Error.Reason.Should().Be(AuthenticationFailureReason.Unauthorized);
        policies.Verify(item => item.GetAsync(It.IsAny<CancellationToken>()), Times.Never);
        updater.VerifyNoOtherCalls();
        events.Should().ContainSingle(item => item.Outcome == SecurityAuditOutcome.Denied);
    }

    [Fact]
    public async Task Another_account_cannot_set_a_password_for_the_current_user()
    {
        Mock<IUserAccountUpdater> updater = new();
        Mock<IAuthenticationSecurityPolicyProvider> policies = new();
        List<SecurityAuditEvent> events = [];
        SetFirstPasswordHandler handler = CreateHandler(updater, RecentSession(), events, policies);

        AuthenticationResult<bool> result = await handler.Handle(new("another-account", NewPassword), CancellationToken.None);

        result.Should().BeOfType<AuthenticationResult<bool>.Failure>()
            .Which.Error.Reason.Should().Be(AuthenticationFailureReason.Unauthorized);
        policies.Verify(item => item.GetAsync(It.IsAny<CancellationToken>()), Times.Never);
        updater.VerifyNoOtherCalls();
        events.Should().ContainSingle(item => item.Outcome == SecurityAuditOutcome.Denied);
        AuditText(events).Should().NotContain(NewPassword);
    }

    [Fact]
    public async Task Effective_password_policy_rejects_before_mutation_and_keeps_the_candidate_out_of_audit()
    {
        const string invalidPassword = "short";
        Mock<IUserAccountUpdater> updater = new();
        List<SecurityAuditEvent> events = [];
        SetFirstPasswordHandler handler = CreateHandler(updater, RecentSession(), events);

        AuthenticationResult<bool> result = await handler.Handle(new(UserId, invalidPassword), CancellationToken.None);

        result.Should().BeOfType<AuthenticationResult<bool>.Failure>()
            .Which.Error.Reason.Should().Be(AuthenticationFailureReason.InvalidInput);
        updater.VerifyNoOtherCalls();
        events.Should().ContainSingle(item => item.RedactedReason == AuthenticationSecurityAuditEvents.FailurePasswordPolicyRejected);
        AuditText(events).Should().NotContain(invalidPassword);
    }

    [Fact]
    public async Task Password_is_added_only_after_the_current_account_is_confirmed_passwordless()
    {
        Mock<IUserAccountUpdater> updater = new();
        updater.Setup(item => item.UserHasPasswordAsync(UserId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        updater.Setup(item => item.AddFirstPasswordAsync(UserId, NewPassword, It.IsAny<CancellationToken>()))
            .ReturnsAsync(PasswordChangeOutcome.Success());
        List<SecurityAuditEvent> events = [];
        SetFirstPasswordHandler handler = CreateHandler(updater, RecentSession(), events);

        AuthenticationResult<bool> result = await handler.Handle(new(UserId, NewPassword), CancellationToken.None);

        result.Should().BeOfType<AuthenticationResult<bool>.Success>().Which.Value.Should().BeTrue();
        updater.Verify(item => item.UserHasPasswordAsync(UserId, It.IsAny<CancellationToken>()), Times.Once);
        updater.Verify(item => item.AddFirstPasswordAsync(UserId, NewPassword, It.IsAny<CancellationToken>()), Times.Once);
        events.Should().ContainSingle(item =>
            item.Outcome == SecurityAuditOutcome.Succeeded &&
            item.EventName == AuthenticationSecurityAuditEvents.PasswordChange &&
            item.Operation == AuthenticationSecurityAuditEvents.FirstPasswordSetOperation);
        AuditText(events).Should().NotContain(NewPassword);
    }

    [Fact]
    public async Task Concurrent_first_password_winner_is_reported_as_conflict_without_a_secret_in_audit()
    {
        Mock<IUserAccountUpdater> updater = new();
        updater.Setup(item => item.UserHasPasswordAsync(UserId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        updater.Setup(item => item.AddFirstPasswordAsync(UserId, NewPassword, It.IsAny<CancellationToken>()))
            .ReturnsAsync(PasswordChangeOutcome.Failure(PasswordChangeStatus.AlreadyHasPassword));
        List<SecurityAuditEvent> events = [];
        SetFirstPasswordHandler handler = CreateHandler(updater, RecentSession(), events);

        AuthenticationResult<bool> result = await handler.Handle(new(UserId, NewPassword), CancellationToken.None);

        result.Should().BeOfType<AuthenticationResult<bool>.Failure>()
            .Which.Error.Reason.Should().Be(AuthenticationFailureReason.Conflict);
        events.Should().ContainSingle(item => item.Outcome == SecurityAuditOutcome.Denied);
        AuditText(events).Should().NotContain(NewPassword);
    }

    private static SetFirstPasswordHandler CreateHandler(
        Mock<IUserAccountUpdater> updater,
        Mock<IAuthenticationSessionRegistry> sessions,
        List<SecurityAuditEvent> events,
        Mock<IAuthenticationSecurityPolicyProvider>? policies = null)
    {
        policies ??= new Mock<IAuthenticationSecurityPolicyProvider>();
        policies.Setup(item => item.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(SecurityAccessPolicy.Default);
        Mock<ISecurityAuditWriter> writer = new();
        writer.Setup(item => item.WriteAsync(It.IsAny<SecurityAuditEvent>(), It.IsAny<CancellationToken>()))
            .Callback<SecurityAuditEvent, CancellationToken>((item, _) => events.Add(item))
            .ReturnsAsync(SecurityAuditWriteResult.Recorded);
        AuthenticationAttemptAuditor auditor = new(writer.Object, new TestRequestContext(), new TestClock());
        return new SetFirstPasswordHandler(
            updater.Object,
            sessions.Object,
            policies.Object,
            new TestClock(),
            new TestRequestContext(),
            auditor);
    }

    private static Mock<IAuthenticationSessionRegistry> RecentSession() => SessionAt(Now);

    private static Mock<IAuthenticationSessionRegistry> SessionAt(DateTime? authenticatedAt)
    {
        Mock<IAuthenticationSessionRegistry> sessions = new();
        sessions.Setup(item => item.GetCurrentAuthenticationAtAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(authenticatedAt);
        return sessions;
    }

    private static string AuditText(IEnumerable<SecurityAuditEvent> events) => JsonSerializer.Serialize(events);

    private sealed class TestRequestContext : IRequestContext
    {
        public string Authority => "test";
        public string? ClientId => null;
        public string? OwnerId => null;
        public string? UserId => UserIdValue;
        public string? TenantId => "tenant-a";
        public string? ResourceId => null;
        public string? TraceId => "trace";
        public string? RequestId => "request";
        public string? AdditionalInfo => null;
        public System.Security.Claims.ClaimsPrincipal? User => null;
        public Request GetRequest() => new() { RequestId = RequestId!, Authority = Authority, TenantId = TenantId, UserId = UserId };
        private const string UserIdValue = SetFirstPasswordHandlerTests.UserId;
    }

    private sealed class TestClock : IAuthenticationUtcClock
    {
        public DateTime UtcNow => Now;
    }
}
