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
using Vitarish.Authentication.Domain.Methods;
using Vitarish.Authentication.Domain.Security;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Requests.Domain.Entities;

namespace Vitarish.Authentication.Tests.Handlers;

public sealed class ChangeTwoStepSignInHandlerTests
{
    private const string UserId = "account-42";
    private const string ConfirmedPhone = "+989121234567";
    private const string ConfirmedEmail = "person@example.test";
    private static readonly DateTime Now = new(2026, 11, 6, 8, 30, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Enabling_or_disabling_requires_a_recent_authentication(bool enabled)
    {
        Mock<IUserAccountUpdater> updater = new();
        Mock<IAuthenticationUserDirectory> users = new();
        Mock<IAuthenticationSessionRegistry> sessions = SessionAt(Now - RecentAuthenticationPolicy.MaximumAge - TimeSpan.FromTicks(1));
        List<SecurityAuditEvent> events = [];
        ChangeTwoStepSignInHandler handler = CreateHandler(updater, users, sessions, events);

        AuthenticationResult<bool> result = await handler.Handle(new(UserId, enabled), CancellationToken.None);

        result.Should().BeOfType<AuthenticationResult<bool>.Failure>()
            .Which.Error.Reason.Should().Be(AuthenticationFailureReason.Unauthorized);
        users.Verify(item => item.FindByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        updater.VerifyNoOtherCalls();
        events.Should().ContainSingle(item => item.Outcome == SecurityAuditOutcome.Denied);
        events.Single().Operation.Should().Be(enabled
            ? AuthenticationSecurityAuditEvents.TwoStepSignInEnableOperation
            : AuthenticationSecurityAuditEvents.TwoStepSignInDisableOperation);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Enabling_or_disabling_is_rejected_when_the_authentication_timestamp_is_missing(bool enabled)
    {
        Mock<IUserAccountUpdater> updater = new();
        Mock<IAuthenticationUserDirectory> users = new();
        Mock<IAuthenticationSessionRegistry> sessions = SessionAt(null);
        List<SecurityAuditEvent> events = [];
        ChangeTwoStepSignInHandler handler = CreateHandler(updater, users, sessions, events);

        AuthenticationResult<bool> result = await handler.Handle(new(UserId, enabled), CancellationToken.None);

        result.Should().BeOfType<AuthenticationResult<bool>.Failure>()
            .Which.Error.Reason.Should().Be(AuthenticationFailureReason.Unauthorized);
        users.Verify(item => item.FindByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        updater.VerifyNoOtherCalls();
        events.Should().ContainSingle(item => item.Outcome == SecurityAuditOutcome.Denied);
    }

    [Fact]
    public async Task Enabling_is_denied_until_a_password_exists()
    {
        AuthenticationUserDescriptor user = Account(hasPassword: false, emailConfirmed: true, phoneConfirmed: true);
        Mock<IUserAccountUpdater> updater = new();
        Mock<IAuthenticationUserDirectory> users = Directory(user);
        List<SecurityAuditEvent> events = [];
        ChangeTwoStepSignInHandler handler = CreateHandler(updater, users, RecentSession(), events);

        AuthenticationResult<bool> result = await handler.Handle(new(UserId, true), CancellationToken.None);

        result.Should().BeOfType<AuthenticationResult<bool>.Failure>()
            .Which.Error.Reason.Should().Be(AuthenticationFailureReason.NotAllowed);
        updater.VerifyNoOtherCalls();
        events.Should().ContainSingle(item => item.Outcome == SecurityAuditOutcome.Denied);
    }

    [Fact]
    public async Task Enabling_is_denied_without_a_confirmed_delivery_channel()
    {
        Mock<IUserAccountUpdater> updater = new();
        Mock<IAuthenticationUserDirectory> users = Directory(Account(hasPassword: true));
        List<SecurityAuditEvent> events = [];
        ChangeTwoStepSignInHandler handler = CreateHandler(updater, users, RecentSession(), events);

        AuthenticationResult<bool> result = await handler.Handle(new(UserId, true), CancellationToken.None);

        result.Should().BeOfType<AuthenticationResult<bool>.Failure>()
            .Which.Error.Reason.Should().Be(AuthenticationFailureReason.NotAllowed);
        updater.VerifyNoOtherCalls();
        events.Should().ContainSingle(item => item.Outcome == SecurityAuditOutcome.Denied);
    }

    [Fact]
    public async Task Confirmed_phone_is_preferred_and_its_policy_action_must_be_allowed()
    {
        AuthenticationUserDescriptor user = Account(hasPassword: true, emailConfirmed: true, phoneConfirmed: true);
        SecurityAccessPolicy policy = PolicyWithout(PreAuthenticationAction.PasswordPlusPhoneOtp);
        Mock<IUserAccountUpdater> updater = new();
        Mock<IAuthenticationUserDirectory> users = Directory(user);
        Mock<IAuthenticationSecurityPolicyProvider> policies = PolicyProvider(policy);
        List<SecurityAuditEvent> events = [];
        ChangeTwoStepSignInHandler handler = CreateHandler(updater, users, RecentSession(), events, policies);

        AuthenticationResult<bool> result = await handler.Handle(new(UserId, true), CancellationToken.None);

        result.Should().BeOfType<AuthenticationResult<bool>.Failure>()
            .Which.Error.Reason.Should().Be(AuthenticationFailureReason.NotAllowed);
        updater.VerifyNoOtherCalls();
        events.Should().ContainSingle(item => item.Outcome == SecurityAuditOutcome.Denied);
        AuditText(events).Should().NotContain(ConfirmedPhone).And.NotContain(ConfirmedEmail);
    }

    [Fact]
    public async Task Confirmed_email_policy_action_must_be_allowed_when_email_is_the_selected_channel()
    {
        AuthenticationUserDescriptor user = Account(hasPassword: true, emailConfirmed: true, phoneConfirmed: false);
        SecurityAccessPolicy policy = PolicyWithout(PreAuthenticationAction.PasswordPlusEmailOtp);
        Mock<IUserAccountUpdater> updater = new();
        Mock<IAuthenticationUserDirectory> users = Directory(user);
        Mock<IAuthenticationSecurityPolicyProvider> policies = PolicyProvider(policy);
        List<SecurityAuditEvent> events = [];
        ChangeTwoStepSignInHandler handler = CreateHandler(updater, users, RecentSession(), events, policies);

        AuthenticationResult<bool> result = await handler.Handle(new(UserId, true), CancellationToken.None);

        result.Should().BeOfType<AuthenticationResult<bool>.Failure>()
            .Which.Error.Reason.Should().Be(AuthenticationFailureReason.NotAllowed);
        updater.VerifyNoOtherCalls();
        events.Should().ContainSingle(item => item.Outcome == SecurityAuditOutcome.Denied);
        AuditText(events).Should().NotContain(ConfirmedEmail);
    }

    [Fact]
    public async Task Enable_uses_the_confirmed_phone_when_both_contacts_are_confirmed()
    {
        AuthenticationUserDescriptor user = Account(hasPassword: true, emailConfirmed: true, phoneConfirmed: true);
        Mock<IUserAccountUpdater> updater = new();
        updater.Setup(item => item.SetTwoFactorEnabledAsync(
                UserId,
                true,
                false,
                AuthenticationMethod.Phone,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        Mock<IAuthenticationUserDirectory> users = Directory(user);
        List<SecurityAuditEvent> events = [];
        ChangeTwoStepSignInHandler handler = CreateHandler(updater, users, RecentSession(), events);

        AuthenticationResult<bool> result = await handler.Handle(new(UserId, true), CancellationToken.None);

        result.Should().BeOfType<AuthenticationResult<bool>.Success>().Which.Value.Should().BeTrue();
        updater.Verify(item => item.SetTwoFactorEnabledAsync(
            UserId,
            true,
            false,
            AuthenticationMethod.Phone,
            It.IsAny<CancellationToken>()), Times.Once);
        events.Should().ContainSingle(item =>
            item.EventName == AuthenticationSecurityAuditEvents.SecurityAttributesChanged &&
            item.Operation == AuthenticationSecurityAuditEvents.TwoStepSignInEnableOperation &&
            item.Outcome == SecurityAuditOutcome.Succeeded);
        AuditText(events).Should().NotContain(ConfirmedPhone).And.NotContain(ConfirmedEmail);
    }

    [Fact]
    public async Task Enable_uses_confirmed_email_when_no_confirmed_phone_exists()
    {
        AuthenticationUserDescriptor user = Account(hasPassword: true, emailConfirmed: true, phoneConfirmed: false);
        Mock<IUserAccountUpdater> updater = new();
        updater.Setup(item => item.SetTwoFactorEnabledAsync(
                UserId,
                true,
                false,
                AuthenticationMethod.Email,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        Mock<IAuthenticationUserDirectory> users = Directory(user);
        List<SecurityAuditEvent> events = [];
        ChangeTwoStepSignInHandler handler = CreateHandler(updater, users, RecentSession(), events);

        AuthenticationResult<bool> result = await handler.Handle(new(UserId, true), CancellationToken.None);

        result.Should().BeOfType<AuthenticationResult<bool>.Success>().Which.Value.Should().BeTrue();
        updater.Verify(item => item.SetTwoFactorEnabledAsync(
            UserId,
            true,
            false,
            AuthenticationMethod.Email,
            It.IsAny<CancellationToken>()), Times.Once);
        events.Should().ContainSingle(item =>
            item.Operation == AuthenticationSecurityAuditEvents.TwoStepSignInEnableOperation &&
            item.Outcome == SecurityAuditOutcome.Succeeded);
        AuditText(events).Should().NotContain(ConfirmedEmail);
    }

    [Fact]
    public async Task Disabling_needs_recent_authentication_but_not_a_password_channel_or_enable_policy()
    {
        AuthenticationUserDescriptor user = Account(
            hasPassword: false,
            emailConfirmed: false,
            phoneConfirmed: false,
            twoFactorEnabled: true);
        Mock<IUserAccountUpdater> updater = new();
        updater.Setup(item => item.SetTwoFactorEnabledAsync(
                UserId,
                false,
                true,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        Mock<IAuthenticationUserDirectory> users = Directory(user);
        Mock<IAuthenticationSecurityPolicyProvider> policies = new();
        List<SecurityAuditEvent> events = [];
        ChangeTwoStepSignInHandler handler = CreateHandler(updater, users, RecentSession(), events, policies);

        AuthenticationResult<bool> result = await handler.Handle(new(UserId, false), CancellationToken.None);

        result.Should().BeOfType<AuthenticationResult<bool>.Success>().Which.Value.Should().BeTrue();
        policies.Verify(item => item.GetAsync(It.IsAny<CancellationToken>()), Times.Never);
        updater.Verify(item => item.SetTwoFactorEnabledAsync(
            UserId,
            false,
            true,
            null,
            It.IsAny<CancellationToken>()), Times.Once);
        events.Should().ContainSingle(item =>
            item.Operation == AuthenticationSecurityAuditEvents.TwoStepSignInDisableOperation &&
            item.Outcome == SecurityAuditOutcome.Succeeded);
    }

    [Fact]
    public async Task Concurrent_security_state_change_is_reported_as_conflict()
    {
        AuthenticationUserDescriptor user = Account(hasPassword: true, emailConfirmed: true, phoneConfirmed: true);
        Mock<IUserAccountUpdater> updater = new();
        updater.Setup(item => item.SetTwoFactorEnabledAsync(
                UserId,
                true,
                false,
                AuthenticationMethod.Phone,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        List<SecurityAuditEvent> events = [];
        ChangeTwoStepSignInHandler handler = CreateHandler(
            updater,
            Directory(user),
            RecentSession(),
            events);

        AuthenticationResult<bool> result = await handler.Handle(new(UserId, true), CancellationToken.None);

        result.Should().BeOfType<AuthenticationResult<bool>.Failure>()
            .Which.Error.Reason.Should().Be(AuthenticationFailureReason.Conflict);
        events.Should().ContainSingle(item => item.Outcome == SecurityAuditOutcome.Denied);
    }

    private static ChangeTwoStepSignInHandler CreateHandler(
        Mock<IUserAccountUpdater> updater,
        Mock<IAuthenticationUserDirectory> users,
        Mock<IAuthenticationSessionRegistry> sessions,
        List<SecurityAuditEvent> events,
        Mock<IAuthenticationSecurityPolicyProvider>? policies = null)
    {
        policies ??= PolicyProvider(SecurityAccessPolicy.Default);
        Mock<ISecurityAuditWriter> writer = new();
        writer.Setup(item => item.WriteAsync(It.IsAny<SecurityAuditEvent>(), It.IsAny<CancellationToken>()))
            .Callback<SecurityAuditEvent, CancellationToken>((item, _) => events.Add(item))
            .ReturnsAsync(SecurityAuditWriteResult.Recorded);
        AuthenticationAttemptAuditor auditor = new(writer.Object, new TestRequestContext(), new TestClock());
        return new ChangeTwoStepSignInHandler(
            updater.Object,
            users.Object,
            sessions.Object,
            policies.Object,
            new TestClock(),
            new TestRequestContext(),
            auditor);
    }

    private static AuthenticationUserDescriptor Account(
        bool hasPassword,
        bool emailConfirmed = false,
        bool phoneConfirmed = false,
        bool twoFactorEnabled = false) =>
        new(
            UserId,
            UserId,
            "Account",
            "Given",
            "Family",
            ConfirmedEmail,
            ConfirmedPhone,
            false,
            emailConfirmed,
            phoneConfirmed,
            hasPassword,
            twoFactorEnabled);

    private static Mock<IAuthenticationUserDirectory> Directory(AuthenticationUserDescriptor user)
    {
        Mock<IAuthenticationUserDirectory> users = new();
        users.Setup(item => item.FindByIdAsync(UserId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        return users;
    }

    private static Mock<IAuthenticationSessionRegistry> RecentSession() => SessionAt(Now);

    private static Mock<IAuthenticationSessionRegistry> SessionAt(DateTime? authenticatedAt)
    {
        Mock<IAuthenticationSessionRegistry> sessions = new();
        sessions.Setup(item => item.GetCurrentAuthenticationAtAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(authenticatedAt);
        return sessions;
    }

    private static Mock<IAuthenticationSecurityPolicyProvider> PolicyProvider(SecurityAccessPolicy policy)
    {
        Mock<IAuthenticationSecurityPolicyProvider> policies = new();
        policies.Setup(item => item.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(policy);
        return policies;
    }

    private static SecurityAccessPolicy PolicyWithout(PreAuthenticationAction action)
    {
        HashSet<PreAuthenticationAction> permitted = SecurityAccessPolicy.Default.PermittedPreAuthenticationActions.ToHashSet();
        _ = permitted.Remove(action);
        return SecurityAccessPolicy.Default with { PermittedPreAuthenticationActions = permitted };
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
        private const string UserIdValue = ChangeTwoStepSignInHandlerTests.UserId;
    }

    private sealed class TestClock : IAuthenticationUtcClock
    {
        public DateTime UtcNow => Now;
    }
}
