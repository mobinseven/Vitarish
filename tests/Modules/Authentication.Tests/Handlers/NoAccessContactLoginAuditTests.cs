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
using Vitarish.Authentication.Domain.Challenges;
using Vitarish.Authentication.Domain.Security;
using Vitarish.Authentication.Infrastructure;
using Vitarish.Authentication.Infrastructure.Options;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Authentication.Tests.Handlers;

public sealed class NoAccessContactLoginAuditTests
{
    [Fact]
    public async Task No_access_phone_login_is_denied_with_its_own_audit_reason()
    {
        AuthenticationChallenge challenge = AuthenticationChallengePolicy.Default.CreatePhoneLoginChallenge(
            "user-42", "+989121234567", DateTimeOffset.UtcNow);
        List<SecurityAuditEvent> events = [];
        Mock<IAuthenticationSignInService> signIn = NoAccessSignIn();
        CompletePhoneLoginHandler handler = CreatePhoneHandler(challenge, signIn, events);

        AuthenticationResult<LoginResult> result = await handler.Handle(
            new CompletePhoneLoginCommand(challenge.Id.Value, "123456", "/home"),
            CancellationToken.None);

        result.Should().BeOfType<AuthenticationResult<LoginResult>.Failure>()
            .Which.Error.Reason.Should().Be(AuthenticationFailureReason.NoAccess);
        events.Should().ContainSingle(audit =>
            audit.Outcome == SecurityAuditOutcome.Denied &&
            audit.SubjectId == "user-42" &&
            audit.RedactedReason == SecurityAuditReasonCatalog.NoAccess);
        events.Should().NotContain(audit =>
            audit.RedactedReason == SecurityAuditReasonCatalog.AuthenticationFailed ||
            audit.RedactedReason == SecurityAuditReasonCatalog.AccountDisabled);
    }

    [Fact]
    public async Task No_access_email_login_is_denied_with_its_own_audit_reason()
    {
        AuthenticationChallenge challenge = AuthenticationChallengePolicy.Default.CreateEmailLoginChallenge(
            "user-42", "user-42@example.test", DateTimeOffset.UtcNow);
        List<SecurityAuditEvent> events = [];
        Mock<IAuthenticationSignInService> signIn = NoAccessSignIn();
        CompleteEmailLoginHandler handler = CreateEmailHandler(challenge, signIn, events);

        AuthenticationResult<LoginResult> result = await handler.Handle(
            new CompleteEmailLoginCommand(challenge.Id.Value, "123456", "/home"),
            CancellationToken.None);

        result.Should().BeOfType<AuthenticationResult<LoginResult>.Failure>()
            .Which.Error.Reason.Should().Be(AuthenticationFailureReason.NoAccess);
        events.Should().ContainSingle(audit =>
            audit.Outcome == SecurityAuditOutcome.Denied &&
            audit.SubjectId == "user-42" &&
            audit.RedactedReason == SecurityAuditReasonCatalog.NoAccess);
        events.Should().NotContain(audit =>
            audit.RedactedReason == SecurityAuditReasonCatalog.AuthenticationFailed ||
            audit.RedactedReason == SecurityAuditReasonCatalog.AccountDisabled);
    }

    [Fact]
    public async Task A_wrong_phone_code_for_an_account_without_access_is_a_generic_failure()
    {
        AuthenticationChallenge challenge = AuthenticationChallengePolicy.Default.CreatePhoneLoginChallenge(
            "user-42", "+989121234567", DateTimeOffset.UtcNow);
        List<SecurityAuditEvent> events = [];
        Mock<IAuthenticationSignInService> signIn = NoAccessSignIn();
        CompletePhoneLoginHandler handler = CreatePhoneHandler(challenge, signIn, events);

        AuthenticationResult<LoginResult> result = await handler.Handle(
            new CompletePhoneLoginCommand(challenge.Id.Value, "000000", "/home"),
            CancellationToken.None);

        result.Should().BeOfType<AuthenticationResult<LoginResult>.Failure>()
            .Which.Error.Reason.Should().Be(AuthenticationFailureReason.InvalidVerificationCode);
        signIn.Verify(
            item => item.SignInAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task A_wrong_email_code_for_an_account_without_access_is_a_generic_failure()
    {
        AuthenticationChallenge challenge = AuthenticationChallengePolicy.Default.CreateEmailLoginChallenge(
            "user-42", "user-42@example.test", DateTimeOffset.UtcNow);
        List<SecurityAuditEvent> events = [];
        Mock<IAuthenticationSignInService> signIn = NoAccessSignIn();
        CompleteEmailLoginHandler handler = CreateEmailHandler(challenge, signIn, events);

        AuthenticationResult<LoginResult> result = await handler.Handle(
            new CompleteEmailLoginCommand(challenge.Id.Value, "000000", "/home"),
            CancellationToken.None);

        result.Should().BeOfType<AuthenticationResult<LoginResult>.Failure>()
            .Which.Error.Reason.Should().Be(AuthenticationFailureReason.InvalidVerificationCode);
        signIn.Verify(
            item => item.SignInAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static Mock<IAuthenticationSignInService> NoAccessSignIn()
    {
        Mock<IAuthenticationSignInService> signIn = new();
        signIn.Setup(item => item.SignInAsync("user-42", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(AuthenticationSessionSignInResult.Failed(
                new AuthenticationFailure(AuthenticationFailureReason.NoAccess, "The account has no access level.")));
        return signIn;
    }

    private static CompletePhoneLoginHandler CreatePhoneHandler(
        AuthenticationChallenge challenge,
        Mock<IAuthenticationSignInService> signIn,
        List<SecurityAuditEvent> events) => new(
        CreateStore(challenge),
        CreateVerifier(challenge),
        signIn.Object,
        CreateRedirects(),
        new InMemoryAuthenticationAttemptTracker(),
        CreatePolicyProvider(),
        CreateKeys(),
        CreateAuditor(events));

    private static CompleteEmailLoginHandler CreateEmailHandler(
        AuthenticationChallenge challenge,
        Mock<IAuthenticationSignInService> signIn,
        List<SecurityAuditEvent> events) => new(
        CreateStore(challenge),
        CreateVerifier(challenge),
        signIn.Object,
        CreateRedirects(),
        new InMemoryAuthenticationAttemptTracker(),
        CreatePolicyProvider(),
        CreateKeys(),
        CreateAuditor(events));

    private static IAuthenticationChallengeStore CreateStore(AuthenticationChallenge challenge)
    {
        Mock<IAuthenticationChallengeStore> store = new();
        store.Setup(item => item.GetAsync(challenge.Id, It.IsAny<CancellationToken>())).ReturnsAsync(challenge);
        store.Setup(item => item.TryReserveAttemptAsync(challenge, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        store.Setup(item => item.TryConsumeAsync(challenge, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        store.Setup(item => item.TryRegisterFailureAsync(challenge, It.IsAny<CancellationToken>())).ReturnsAsync(1);
        return store.Object;
    }

    private static IAuthenticationCodeVerifier CreateVerifier(AuthenticationChallenge challenge)
    {
        Mock<IAuthenticationCodeVerifier> verifier = new();
        verifier.Setup(item => item.VerifyAsync("user-42", challenge.TokenPurpose, "123456", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        return verifier.Object;
    }

    private static IAuthenticationRedirectResolver CreateRedirects()
    {
        Mock<IAuthenticationRedirectResolver> redirects = new();
        redirects.Setup(item => item.ResolveAsync("/home", false, "user-42", It.IsAny<CancellationToken>()))
            .ReturnsAsync("/home");
        return redirects.Object;
    }

    private static IAuthenticationSecurityPolicyProvider CreatePolicyProvider()
    {
        Mock<IAuthenticationSecurityPolicyProvider> provider = new();
        provider.Setup(item => item.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(SecurityAccessPolicy.Default);
        return provider.Object;
    }

    private static IAuthenticationAttemptKeyFactory CreateKeys() => new AuthenticationAttemptKeyFactory(
        Options.Create(new AuthenticationAttemptKeyOptions
        {
            Realm = "tests",
            CurrentPepper = "test-authentication-attempt-key-pepper-20260829"
        }));

    private static AuthenticationAttemptAuditor CreateAuditor(List<SecurityAuditEvent> events)
    {
        Mock<ISecurityAuditWriter> writer = new();
        writer.Setup(item => item.WriteAsync(It.IsAny<SecurityAuditEvent>(), It.IsAny<CancellationToken>()))
            .Callback<SecurityAuditEvent, CancellationToken>((audit, _) => events.Add(audit))
            .ReturnsAsync(SecurityAuditWriteResult.Recorded);
        Mock<IRequestContext> requestContext = new();
        requestContext.SetupGet(item => item.TenantId).Returns("tenant-a");
        requestContext.SetupGet(item => item.RequestId).Returns("no-access-request");
        requestContext.SetupGet(item => item.TraceId).Returns("no-access-trace");
        return new AuthenticationAttemptAuditor(writer.Object, requestContext.Object, new TestAuthenticationUtcClock());
    }

    private sealed class TestAuthenticationUtcClock : IAuthenticationUtcClock
    {
        public DateTime UtcNow => new(2026, 9, 28, 8, 0, 0, DateTimeKind.Utc);
    }
}
