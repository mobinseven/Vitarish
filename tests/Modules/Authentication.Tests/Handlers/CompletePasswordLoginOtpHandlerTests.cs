using System.Security.Claims;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Application.Commands;
using Vitarish.Authentication.Application.Handlers;
using Vitarish.Authentication.Application.Results;
using Vitarish.Authentication.Domain.Challenges;
using Vitarish.Authentication.Domain.Security;
using Vitarish.Authentication.Infrastructure;
using Vitarish.Authentication.Infrastructure.Options;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Requests.Domain.Entities;
using Xunit;

namespace Vitarish.Authentication.Tests.Handlers;

public sealed class CompletePasswordLoginOtpHandlerTests
{
    [Fact]
    public async Task Successful_completion_requires_a_completed_audit_record()
    {
        TestDependencies dependencies = CreateDependencies();
        List<SecurityAuditEvent> events = [];
        CompletePasswordLoginOtpHandler handler = dependencies.CreateHandler(new RecordingWriter(events));

        AuthenticationResult<LoginResult> result = await handler.Handle(Command(), CancellationToken.None);

        result.Should().BeOfType<AuthenticationResult<LoginResult>.Success>();
        events.Select(x => x.Outcome).Should().ContainInOrder(SecurityAuditOutcome.Started, SecurityAuditOutcome.Succeeded);
        events[^1].EventName.Should().Be("authentication.password.login.otp");
        dependencies.SignIn.Verify(x => x.SignInAsync("user-42", false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Completion_audit_failure_preserves_success()
    {
        TestDependencies dependencies = CreateDependencies();
        Mock<ISecurityAuditWriter> writer = new();
        writer.SetupSequence(x => x.WriteAsync(It.IsAny<SecurityAuditEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SecurityAuditWriteResult.Recorded)
            .ThrowsAsync(new SecurityAuditStoreException("offline", new InvalidOperationException()));
        CompletePasswordLoginOtpHandler handler = dependencies.CreateHandler(writer.Object);

        AuthenticationResult<LoginResult> result = await handler.Handle(Command(), CancellationToken.None);

        result.Should().BeOfType<AuthenticationResult<LoginResult>.Success>();
        dependencies.SignIn.Verify(x => x.SignInAsync("user-42", false, It.IsAny<CancellationToken>()), Times.Once);
        dependencies.SignIn.Verify(x => x.SignOutAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Concurrent_successful_completions_sign_in_only_once()
    {
        InMemoryAuthenticationChallengeStore store = new();
        AuthenticationChallenge challenge = AuthenticationChallengePolicy.Default.CreatePasswordLoginOtpChallenge(
            "user-42", "+989121234567", DateTimeOffset.UtcNow, false, false);
        await store.SaveAsync(challenge, CancellationToken.None);

        Mock<IAuthenticationCodeVerifier> verifier = new();
        verifier.Setup(x => x.VerifyAsync("user-42", challenge.TokenPurpose, "123456", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        Mock<IAuthenticationSignInService> signIn = new();
        Mock<IAuthenticationRedirectResolver> redirects = new();
        redirects.Setup(x => x.ResolveAsync("/home", false, "user-42", It.IsAny<CancellationToken>())).ReturnsAsync("/home");
        Mock<ISecurityAuditWriter> audit = new();
        audit.Setup(x => x.WriteAsync(It.IsAny<SecurityAuditEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SecurityAuditWriteResult.Recorded);
        CompletePasswordLoginOtpHandler handler = new(
            store,
            verifier.Object,
            signIn.Object,
            redirects.Object,
            audit.Object,
            new TestRequestContext(),
            new InMemoryAuthenticationAttemptTracker(),
            new TestSecurityPolicyProvider(),
            CreateKeys(),
            new TestAuthenticationUtcClock());

        CompletePasswordLoginOtpCommand command = new(challenge.Id.Value, "123456", "/home");
        AuthenticationResult<LoginResult>[] results = await Task.WhenAll(
            handler.Handle(command, CancellationToken.None),
            handler.Handle(command, CancellationToken.None));

        results.OfType<AuthenticationResult<LoginResult>.Success>().Should().ContainSingle();
        results.OfType<AuthenticationResult<LoginResult>.Failure>().Single().Error.Reason
            .Should().Be(AuthenticationFailureReason.ChallengeAlreadyUsed);
        signIn.Verify(x => x.SignInAsync("user-42", false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Concurrent_invalid_codes_consume_only_the_configured_attempts()
    {
        InMemoryAuthenticationChallengeStore store = new();
        AuthenticationChallenge challenge = AuthenticationChallengePolicy.Default.CreatePasswordLoginOtpChallenge(
            "user-42", "+989121234567", DateTimeOffset.UtcNow, false, false);
        await store.SaveAsync(challenge, CancellationToken.None);

        Mock<IAuthenticationCodeVerifier> verifier = new();
        verifier.Setup(x => x.VerifyAsync("user-42", challenge.TokenPurpose, "000000", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        Mock<IAuthenticationSignInService> signIn = new();
        Mock<IAuthenticationRedirectResolver> redirects = new();
        Mock<ISecurityAuditWriter> audit = new();
        audit.Setup(x => x.WriteAsync(It.IsAny<SecurityAuditEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SecurityAuditWriteResult.Recorded);
        CompletePasswordLoginOtpHandler handler = new(
            store,
            verifier.Object,
            signIn.Object,
            redirects.Object,
            audit.Object,
            new TestRequestContext(),
            new InMemoryAuthenticationAttemptTracker(),
            new TestSecurityPolicyProvider(),
            CreateKeys(),
            new TestAuthenticationUtcClock());

        CompletePasswordLoginOtpCommand command = new(challenge.Id.Value, "000000", "/home");
        AuthenticationResult<LoginResult>[] results = await Task.WhenAll(
            Enumerable.Range(0, 32).Select(_ => handler.Handle(command, CancellationToken.None)));

        results.Should().OnlyContain(result => result is AuthenticationResult<LoginResult>.Failure);
        results.Count(result => result is AuthenticationResult<LoginResult>.Failure failure &&
                               failure.Error.Reason == AuthenticationFailureReason.InvalidVerificationCode)
            .Should().BeLessThanOrEqualTo(challenge.MaximumAttempts - 1);
        results.Count(result => result is AuthenticationResult<LoginResult>.Failure failure &&
                               failure.Error.Reason == AuthenticationFailureReason.LockedOut)
            .Should().BeGreaterThan(0);
        (await store.GetAsync(challenge.Id, CancellationToken.None))!.Status
            .Should().Be(AuthenticationChallengeStatus.Exhausted);
        verifier.Verify(x => x.VerifyAsync("user-42", challenge.TokenPurpose, "000000", It.IsAny<CancellationToken>()),
            Times.Exactly(challenge.MaximumAttempts));
        signIn.Verify(x => x.SignInAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static CompletePasswordLoginOtpCommand Command() => new("challenge-42", "123456", "/home");

    private static TestDependencies CreateDependencies()
    {
        Mock<IAuthenticationChallengeStore> store = new();
        AuthenticationChallenge challenge = AuthenticationChallengePolicy.Default.CreatePasswordLoginOtpChallenge(
            "user-42", "+989121234567", DateTimeOffset.UtcNow, false, false);
        store.Setup(x => x.GetAsync(It.IsAny<AuthenticationChallengeId>(), It.IsAny<CancellationToken>())).ReturnsAsync(challenge);
        store.Setup(x => x.TryReserveAttemptAsync(It.IsAny<AuthenticationChallenge>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        store.Setup(x => x.TryConsumeAsync(It.IsAny<AuthenticationChallenge>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        Mock<IAuthenticationCodeVerifier> verifier = new();
        verifier.Setup(x => x.VerifyAsync("user-42", challenge.TokenPurpose, "123456", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        Mock<IAuthenticationSignInService> signIn = new();
        Mock<IAuthenticationRedirectResolver> redirects = new();
        redirects.Setup(x => x.ResolveAsync("/home", false, "user-42", It.IsAny<CancellationToken>())).ReturnsAsync("/home");
        return new TestDependencies(store, verifier, signIn, redirects);
    }

    private sealed record TestDependencies(
        Mock<IAuthenticationChallengeStore> Store,
        Mock<IAuthenticationCodeVerifier> Verifier,
        Mock<IAuthenticationSignInService> SignIn,
        Mock<IAuthenticationRedirectResolver> Redirects)
    {
        public CompletePasswordLoginOtpHandler CreateHandler(ISecurityAuditWriter writer) => new(
            Store.Object,
            Verifier.Object,
            SignIn.Object,
            Redirects.Object,
            writer,
            new TestRequestContext(),
            new InMemoryAuthenticationAttemptTracker(),
            new TestSecurityPolicyProvider(),
            CreateKeys(),
            new TestAuthenticationUtcClock());
    }

    private sealed class TestSecurityPolicyProvider : IAuthenticationSecurityPolicyProvider
    {
        public Task<SecurityAccessPolicy> GetAsync(CancellationToken cancellationToken) =>
            Task.FromResult(SecurityAccessPolicy.ProductEvaluated);
    }

    private sealed class RecordingWriter(List<SecurityAuditEvent> events) : ISecurityAuditWriter
    {
        public Task<SecurityAuditWriteResult> WriteAsync(SecurityAuditEvent auditEvent, CancellationToken cancellationToken)
        {
            events.Add(auditEvent);
            return Task.FromResult(SecurityAuditWriteResult.Recorded);
        }

        public Task<SecurityAuditWriteResult> UpdateAsync(string tenantId, string idempotencyKey, string outcome, string? reason, CancellationToken cancellationToken) =>
            Task.FromResult(SecurityAuditWriteResult.AlreadyRecorded);
    }

    private sealed class TestRequestContext : IRequestContext
    {
        public string Authority => "test";
        public string? ClientId => null;
        public string? OwnerId => "owner-1";
        public string? UserId => null;
        public string? TenantId => "tenant-a";
        public string? ResourceId => "authentication";
        public string? TraceId => "trace-42";
        public string? RequestId => "request-42";
        public string? AdditionalInfo => null;
        public ClaimsPrincipal? User => new(new ClaimsIdentity());
        public Request GetRequest() => new() { RequestId = RequestId!, Authority = Authority, TenantId = TenantId };
    }

    private static IAuthenticationAttemptKeyFactory CreateKeys() => new AuthenticationAttemptKeyFactory(
        Options.Create(new AuthenticationAttemptKeyOptions
        {
            Realm = "tests",
            CurrentPepper = "test-authentication-attempt-key-pepper-20260829"
        }));

    private sealed class TestAuthenticationUtcClock : IAuthenticationUtcClock
    {
        public DateTime UtcNow => new(2026, 9, 28, 8, 0, 0, DateTimeKind.Utc);
    }
}
