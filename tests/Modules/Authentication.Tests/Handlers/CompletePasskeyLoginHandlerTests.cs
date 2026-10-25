using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Application.Commands;
using Vitarish.Authentication.Application.Handlers;
using Vitarish.Authentication.Application.Results;
using Vitarish.Authentication.Application.Services;
using Vitarish.Authentication.Domain.Methods;
using Vitarish.Authentication.Domain.Security;
using Vitarish.Authentication.Infrastructure;
using Vitarish.Authentication.Infrastructure.Options;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Authentication.Tests.Security;

namespace Vitarish.Authentication.Tests.Handlers;

public sealed class CompletePasskeyLoginHandlerTests
{
    [Fact]
    public async Task Denied_cached_attempt_does_not_call_web_authn_completion()
    {
        Mock<IWebAuthnAuthenticationService> webAuthn = new();
        IAuthenticationAttemptKeyFactory keys = CreateKeys();
        webAuthn.Setup(item => item.GetAttemptKeyAsync("challenge-1", "credential", It.IsAny<CancellationToken>()))
            .ReturnsAsync(keys.Identifier(AuthenticationMethod.Passkey, "user@example.test"));
        Mock<IAuthenticationAttemptTracker> tracker = new();
        tracker.Setup(item => item.IsDeniedAsync(
                keys.Identifier(AuthenticationMethod.Passkey, "user@example.test"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        Mock<IAuthenticationSecurityPolicyProvider> policy = new();
        policy.Setup(item => item.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(SecurityAccessPolicy.Default);

        CompletePasskeyLoginHandler handler = new(
            webAuthn.Object,
            Mock.Of<IAuthenticationSignInService>(),
            Mock.Of<IAuthenticationRedirectResolver>(),
            policy.Object,
            tracker.Object,
            keys,
            TestSourceAttemptGuards.WithoutClientAddress(),
            CreateAttemptAuditor());

        AuthenticationResult<LoginResult> result = await handler.Handle(
            new CompletePasskeyLoginCommand("challenge-1", "credential", "/home"),
            CancellationToken.None);

        result.Should().BeOfType<AuthenticationResult<LoginResult>.Failure>()
            .Which.Error.Reason.Should().Be(AuthenticationFailureReason.LockedOut);
        webAuthn.Verify(item => item.CompleteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        tracker.Verify(item => item.IsDeniedAsync(
            keys.Identifier(AuthenticationMethod.Passkey, "user@example.test"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Failed_completion_registers_the_same_cached_attempt_key()
    {
        IAuthenticationAttemptKeyFactory keys = CreateKeys();
        string attemptKey = keys.Identifier(AuthenticationMethod.Passkey, "user@example.test");
        Mock<IWebAuthnAuthenticationService> webAuthn = new();
        webAuthn.Setup(item => item.GetAttemptKeyAsync("challenge-1", "credential", It.IsAny<CancellationToken>())).ReturnsAsync(attemptKey);
        webAuthn.Setup(item => item.CompleteAsync("challenge-1", "credential", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WebAuthnCompleteResult(false, null, new(AuthenticationFailureReason.InvalidCredential, "failed")));
        Mock<IAuthenticationAttemptTracker> tracker = new();
        tracker.Setup(item => item.IsDeniedAsync(attemptKey, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        tracker.Setup(item => item.IsDeniedAsync(It.Is<string>(key => key != attemptKey), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        tracker.Setup(item => item.RegisterFailureAsync(attemptKey, It.IsAny<int>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>())).ReturnsAsync(1);
        Mock<IAuthenticationSecurityPolicyProvider> policy = new();
        policy.Setup(item => item.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(SecurityAccessPolicy.Default);

        CompletePasskeyLoginHandler handler = new(
            webAuthn.Object,
            Mock.Of<IAuthenticationSignInService>(),
            Mock.Of<IAuthenticationRedirectResolver>(),
            policy.Object,
            tracker.Object,
            keys,
            TestSourceAttemptGuards.WithoutClientAddress(),
            CreateAttemptAuditor());

        AuthenticationResult<LoginResult> result = await handler.Handle(
            new CompletePasskeyLoginCommand("challenge-1", "credential", "/home"),
            CancellationToken.None);

        result.Should().BeOfType<AuthenticationResult<LoginResult>.Failure>();
        tracker.Verify(item => item.RegisterFailureAsync(attemptKey, 5, TimeSpan.FromMinutes(15), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Discoverable_failed_completion_uses_the_credential_derived_attempt_key()
    {
        IAuthenticationAttemptKeyFactory keys = CreateKeys();
        string attemptKey = keys.Identifier(AuthenticationMethod.Passkey, "credential-id");
        Mock<IWebAuthnAuthenticationService> webAuthn = new();
        webAuthn.Setup(item => item.GetAttemptKeyAsync("challenge-1", It.IsAny<CancellationToken>())).ReturnsAsync((string?)null);
        webAuthn.Setup(item => item.GetAttemptKeyAsync("challenge-1", "credential", It.IsAny<CancellationToken>())).ReturnsAsync(attemptKey);
        webAuthn.Setup(item => item.CompleteAsync("challenge-1", "credential", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WebAuthnCompleteResult(false, null, new(AuthenticationFailureReason.InvalidCredential, "failed")));
        Mock<IAuthenticationAttemptTracker> tracker = new();
        tracker.Setup(item => item.IsDeniedAsync(attemptKey, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        tracker.Setup(item => item.IsDeniedAsync(It.Is<string>(key => key != attemptKey), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        tracker.Setup(item => item.RegisterFailureAsync(attemptKey, It.IsAny<int>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>())).ReturnsAsync(1);
        Mock<IAuthenticationSecurityPolicyProvider> policy = new();
        policy.Setup(item => item.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(SecurityAccessPolicy.Default);

        CompletePasskeyLoginHandler handler = new(
            webAuthn.Object,
            Mock.Of<IAuthenticationSignInService>(),
            Mock.Of<IAuthenticationRedirectResolver>(),
            policy.Object,
            tracker.Object,
            keys,
            TestSourceAttemptGuards.WithoutClientAddress(),
            CreateAttemptAuditor());

        _ = await handler.Handle(new CompletePasskeyLoginCommand("challenge-1", "credential", "/home"), CancellationToken.None);

        tracker.Verify(item => item.RegisterFailureAsync(attemptKey, 5, TimeSpan.FromMinutes(15), It.IsAny<CancellationToken>()), Times.Once);
        tracker.Verify(item => item.RegisterFailureAsync(
            keys.Challenge(AuthenticationMethod.Passkey, "challenge-1"),
            5,
            TimeSpan.FromMinutes(15),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Discoverable_completion_cannot_bypass_the_threshold_by_rotating_credential_ids()
    {
        IAuthenticationAttemptKeyFactory keys = CreateKeys();
        Mock<IWebAuthnAuthenticationService> webAuthn = new();
        webAuthn.Setup(item => item.GetAttemptKeyAsync("challenge-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);
        webAuthn.SetupSequence(item => item.GetAttemptKeyAsync("challenge-1", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(keys.Identifier(AuthenticationMethod.Passkey, "credential-id-1"))
            .ReturnsAsync(keys.Identifier(AuthenticationMethod.Passkey, "credential-id-2"));
        webAuthn.Setup(item => item.CompleteAsync("challenge-1", "credential", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WebAuthnCompleteResult(false, null, new(AuthenticationFailureReason.InvalidCredential, "failed")));

        SecurityAccessPolicy policy = SecurityAccessPolicy.Default with { FailedAttemptThreshold = 2 };
        Mock<IAuthenticationSecurityPolicyProvider> policyProvider = new();
        policyProvider.Setup(item => item.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(policy);
        InMemoryAuthenticationAttemptTracker tracker = new();
        CompletePasskeyLoginHandler handler = new(
            webAuthn.Object,
            Mock.Of<IAuthenticationSignInService>(),
            Mock.Of<IAuthenticationRedirectResolver>(),
            policyProvider.Object,
            tracker,
            keys,
            TestSourceAttemptGuards.WithoutClientAddress(),
            CreateAttemptAuditor());

        AuthenticationResult<LoginResult> first = await handler.Handle(
            new CompletePasskeyLoginCommand("challenge-1", "credential", "/home"),
            CancellationToken.None);
        AuthenticationResult<LoginResult> second = await handler.Handle(
            new CompletePasskeyLoginCommand("challenge-1", "credential", "/home"),
            CancellationToken.None);

        first.Should().BeOfType<AuthenticationResult<LoginResult>.Failure>()
            .Which.Error.Reason.Should().Be(AuthenticationFailureReason.InvalidCredential);
        second.Should().BeOfType<AuthenticationResult<LoginResult>.Failure>()
            .Which.Error.Reason.Should().Be(AuthenticationFailureReason.LockedOut);
        webAuthn.Verify(item => item.CompleteAsync("challenge-1", "credential", It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Disabled_account_passkey_login_is_denied_with_its_own_audit_reason()
    {
        IAuthenticationAttemptKeyFactory keys = CreateKeys();
        Mock<IWebAuthnAuthenticationService> webAuthn = new();
        webAuthn.Setup(item => item.CompleteAsync("challenge-1", "credential", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WebAuthnCompleteResult(true, "user-42", null));
        Mock<IAuthenticationAttemptTracker> tracker = new();
        tracker.Setup(item => item.IsDeniedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        Mock<IAuthenticationSecurityPolicyProvider> policy = new();
        policy.Setup(item => item.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(SecurityAccessPolicy.Default);
        Mock<IAuthenticationSignInService> signIn = new();
        signIn.Setup(item => item.SignInAsync("user-42", It.IsAny<CancellationToken>()))
            .ReturnsAsync(AuthenticationSessionSignInResult.Failed(
                new AuthenticationFailure(AuthenticationFailureReason.AccountDisabled, "The account is disabled.")));
        List<SecurityAuditEvent> events = [];
        Mock<ISecurityAuditWriter> writer = new();
        writer.Setup(item => item.WriteAsync(It.IsAny<SecurityAuditEvent>(), It.IsAny<CancellationToken>()))
            .Callback<SecurityAuditEvent, CancellationToken>((audit, _) => events.Add(audit))
            .ReturnsAsync(SecurityAuditWriteResult.Recorded);
        Mock<IRequestContext> requestContext = new();
        requestContext.SetupGet(item => item.TenantId).Returns("tenant-a");
        requestContext.SetupGet(item => item.RequestId).Returns("passkey-request");
        requestContext.SetupGet(item => item.TraceId).Returns("passkey-trace");

        CompletePasskeyLoginHandler handler = new(
            webAuthn.Object,
            signIn.Object,
            Mock.Of<IAuthenticationRedirectResolver>(),
            policy.Object,
            tracker.Object,
            keys,
            TestSourceAttemptGuards.WithoutClientAddress(),
            new AuthenticationAttemptAuditor(writer.Object, requestContext.Object, new TestAuthenticationUtcClock()));

        AuthenticationResult<LoginResult> result = await handler.Handle(
            new CompletePasskeyLoginCommand("challenge-1", "credential", "/home"),
            CancellationToken.None);

        result.Should().BeOfType<AuthenticationResult<LoginResult>.Failure>()
            .Which.Error.Reason.Should().Be(AuthenticationFailureReason.AccountDisabled);
        events.Should().ContainSingle(audit =>
            audit.Outcome == SecurityAuditOutcome.Denied &&
            audit.RedactedReason == SecurityAuditReasonCatalog.AccountDisabled);
        events.Should().NotContain(audit => audit.RedactedReason == SecurityAuditReasonCatalog.AuthenticationFailed);
    }

    [Fact]
    public async Task No_access_passkey_login_is_denied_with_its_own_audit_reason()
    {
        IAuthenticationAttemptKeyFactory keys = CreateKeys();
        Mock<IWebAuthnAuthenticationService> webAuthn = new();
        webAuthn.Setup(item => item.CompleteAsync("challenge-1", "credential", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WebAuthnCompleteResult(true, "user-42", null));
        Mock<IAuthenticationAttemptTracker> tracker = new();
        tracker.Setup(item => item.IsDeniedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        Mock<IAuthenticationSecurityPolicyProvider> policy = new();
        policy.Setup(item => item.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(SecurityAccessPolicy.Default);
        Mock<IAuthenticationSignInService> signIn = new();
        signIn.Setup(item => item.SignInAsync("user-42", It.IsAny<CancellationToken>()))
            .ReturnsAsync(AuthenticationSessionSignInResult.Failed(
                new AuthenticationFailure(AuthenticationFailureReason.NoAccess, "The account has no access level.")));
        List<SecurityAuditEvent> events = [];
        Mock<ISecurityAuditWriter> writer = new();
        writer.Setup(item => item.WriteAsync(It.IsAny<SecurityAuditEvent>(), It.IsAny<CancellationToken>()))
            .Callback<SecurityAuditEvent, CancellationToken>((audit, _) => events.Add(audit))
            .ReturnsAsync(SecurityAuditWriteResult.Recorded);
        Mock<IRequestContext> requestContext = new();
        requestContext.SetupGet(item => item.TenantId).Returns("tenant-a");
        requestContext.SetupGet(item => item.RequestId).Returns("passkey-request");
        requestContext.SetupGet(item => item.TraceId).Returns("passkey-trace");

        CompletePasskeyLoginHandler handler = new(
            webAuthn.Object,
            signIn.Object,
            Mock.Of<IAuthenticationRedirectResolver>(),
            policy.Object,
            tracker.Object,
            keys,
            TestSourceAttemptGuards.WithoutClientAddress(),
            new AuthenticationAttemptAuditor(writer.Object, requestContext.Object, new TestAuthenticationUtcClock()));

        AuthenticationResult<LoginResult> result = await handler.Handle(
            new CompletePasskeyLoginCommand("challenge-1", "credential", "/home"),
            CancellationToken.None);

        result.Should().BeOfType<AuthenticationResult<LoginResult>.Failure>()
            .Which.Error.Reason.Should().Be(AuthenticationFailureReason.NoAccess);
        events.Should().ContainSingle(audit =>
            audit.Outcome == SecurityAuditOutcome.Denied &&
            audit.RedactedReason == SecurityAuditReasonCatalog.NoAccess);
        events.Should().NotContain(audit =>
            audit.RedactedReason == SecurityAuditReasonCatalog.AuthenticationFailed ||
            audit.RedactedReason == SecurityAuditReasonCatalog.AccountDisabled);
    }

    private static IAuthenticationAttemptKeyFactory CreateKeys() => new AuthenticationAttemptKeyFactory(
        Options.Create(new AuthenticationAttemptKeyOptions
        {
            Realm = "tests",
            CurrentPepper = "test-authentication-attempt-key-pepper-20260829"
        }));

    private static AuthenticationAttemptAuditor CreateAttemptAuditor()
    {
        Mock<ISecurityAuditWriter> writer = new();
        writer.Setup(item => item.WriteAsync(It.IsAny<SecurityAuditEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SecurityAuditWriteResult.Recorded);
        Mock<IRequestContext> requestContext = new();
        requestContext.SetupGet(item => item.TenantId).Returns("tenant-a");
        requestContext.SetupGet(item => item.RequestId).Returns("passkey-request");
        requestContext.SetupGet(item => item.TraceId).Returns("passkey-trace");
        return new AuthenticationAttemptAuditor(writer.Object, requestContext.Object, new TestAuthenticationUtcClock());
    }

    private sealed class TestAuthenticationUtcClock : IAuthenticationUtcClock
    {
        public DateTime UtcNow => new(2026, 9, 28, 8, 0, 0, DateTimeKind.Utc);
    }
}
