using System.Security.Claims;
using System.Text.Json;
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
using Vitarish.Authentication.Domain.Methods;
using Vitarish.Authentication.Domain.Security;
using Vitarish.Authentication.Infrastructure;
using Vitarish.Authentication.Infrastructure.Options;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Requests.Domain.Entities;
using Xunit;
using Vitarish.Authentication.Tests.Security;

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
        dependencies.SignIn.Verify(x => x.SignInAsync("user-42", It.IsAny<CancellationToken>()), Times.Once);
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
        dependencies.SignIn.Verify(x => x.SignInAsync("user-42", It.IsAny<CancellationToken>()), Times.Once);
        dependencies.SignIn.Verify(x => x.SignOutAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Concurrent_successful_completions_sign_in_only_once()
    {
        InMemoryAuthenticationChallengeStore store = new();
        AuthenticationChallenge challenge = AuthenticationChallengePolicy.Default.CreatePasswordLoginOtpChallenge(
            "user-42", "+989121234567", DateTimeOffset.UtcNow, false);
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
            TestSourceAttemptGuards.WithoutClientAddress(),
            new TestAuthenticationUtcClock(),
            Mock.Of<IAuthenticationSessionRegistry>(),
            Mock.Of<IAuthenticationUserDirectory>());

        CompletePasswordLoginOtpCommand command = new(challenge.Id.Value, "123456", "/home");
        AuthenticationResult<LoginResult>[] results = await Task.WhenAll(
            handler.Handle(command, CancellationToken.None),
            handler.Handle(command, CancellationToken.None));

        results.OfType<AuthenticationResult<LoginResult>.Success>().Should().ContainSingle();
        results.OfType<AuthenticationResult<LoginResult>.Failure>().Single().Error.Reason
            .Should().Be(AuthenticationFailureReason.ChallengeAlreadyUsed);
        signIn.Verify(x => x.SignInAsync("user-42", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Concurrent_email_factor_completions_sign_in_only_once()
    {
        InMemoryAuthenticationChallengeStore store = new();
        AuthenticationChallenge challenge = AuthenticationChallengePolicy.Default.CreatePasswordLoginEmailOtpChallenge(
            "user-42", "person@example.test", DateTimeOffset.UtcNow, false);
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
        Mock<IAuthenticationUserDirectory> userDirectory = new();
        userDirectory.Setup(x => x.FindByIdAsync("user-42", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthenticationUserDescriptor("user-42", "user-42", null, "Given", "Family", "person@example.test", null, false, EmailConfirmed: true));
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
            TestSourceAttemptGuards.WithoutClientAddress(),
            new TestAuthenticationUtcClock(),
            Mock.Of<IAuthenticationSessionRegistry>(),
            userDirectory.Object);

        CompletePasswordLoginOtpCommand command = new(challenge.Id.Value, "123456", "/home");
        AuthenticationResult<LoginResult>[] results = await Task.WhenAll(
            handler.Handle(command, CancellationToken.None),
            handler.Handle(command, CancellationToken.None));

        results.OfType<AuthenticationResult<LoginResult>.Success>().Should().ContainSingle();
        results.OfType<AuthenticationResult<LoginResult>.Failure>().Single().Error.Reason
            .Should().Be(AuthenticationFailureReason.ChallengeAlreadyUsed);
        signIn.Verify(x => x.SignInAsync("user-42", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Concurrent_invalid_codes_consume_only_the_single_second_factor_attempt()
    {
        InMemoryAuthenticationChallengeStore store = new();
        AuthenticationChallenge challenge = AuthenticationChallengePolicy.Default.CreatePasswordLoginOtpChallenge(
            "user-42", "+989121234567", DateTimeOffset.UtcNow, false);
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
            TestSourceAttemptGuards.WithoutClientAddress(),
            new TestAuthenticationUtcClock(),
            Mock.Of<IAuthenticationSessionRegistry>(),
            Mock.Of<IAuthenticationUserDirectory>());

        CompletePasswordLoginOtpCommand command = new(challenge.Id.Value, "000000", "/home");
        AuthenticationResult<LoginResult>[] results = await Task.WhenAll(
            Enumerable.Range(0, 32).Select(_ => handler.Handle(command, CancellationToken.None)));

        results.Should().OnlyContain(result => result is AuthenticationResult<LoginResult>.Failure);
        results.OfType<AuthenticationResult<LoginResult>.Failure>().Select(failure => failure.Error.Reason)
            .Should().AllBeEquivalentTo(AuthenticationFailureReason.ChallengeAttemptsExhausted);
        (await store.GetAsync(challenge.Id, CancellationToken.None))!.Status
            .Should().Be(AuthenticationChallengeStatus.Exhausted);
        verifier.Verify(x => x.VerifyAsync("user-42", challenge.TokenPurpose, "000000", It.IsAny<CancellationToken>()),
            Times.Once);
        signIn.Verify(x => x.SignInAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task A_wrong_second_factor_code_ends_the_challenge_so_the_user_restarts_sign_in()
    {
        InMemoryAuthenticationChallengeStore store = new();
        AuthenticationChallenge challenge = AuthenticationChallengePolicy.Default.CreatePasswordLoginOtpChallenge(
            "user-42", "+989121234567", DateTimeOffset.UtcNow, false);
        await store.SaveAsync(challenge, CancellationToken.None);

        Mock<IAuthenticationCodeVerifier> verifier = new();
        verifier.Setup(x => x.VerifyAsync("user-42", challenge.TokenPurpose, "000000", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        verifier.Setup(x => x.VerifyAsync("user-42", challenge.TokenPurpose, "123456", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        Mock<IAuthenticationSignInService> signIn = new();
        Mock<ISecurityAuditWriter> audit = new();
        audit.Setup(x => x.WriteAsync(It.IsAny<SecurityAuditEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SecurityAuditWriteResult.Recorded);
        CompletePasswordLoginOtpHandler handler = new(
            store,
            verifier.Object,
            signIn.Object,
            new Mock<IAuthenticationRedirectResolver>().Object,
            audit.Object,
            new TestRequestContext(),
            new InMemoryAuthenticationAttemptTracker(),
            new TestSecurityPolicyProvider(),
            CreateKeys(),
            TestSourceAttemptGuards.WithoutClientAddress(),
            new TestAuthenticationUtcClock(),
            Mock.Of<IAuthenticationSessionRegistry>(),
            Mock.Of<IAuthenticationUserDirectory>());

        AuthenticationResult<LoginResult> wrongCode = await handler.Handle(new(challenge.Id.Value, "000000", "/home"), CancellationToken.None);
        AuthenticationResult<LoginResult> correctCodeAfterwards = await handler.Handle(new(challenge.Id.Value, "123456", "/home"), CancellationToken.None);

        wrongCode.Should().BeOfType<AuthenticationResult<LoginResult>.Failure>()
            .Which.Error.Reason.Should().Be(AuthenticationFailureReason.ChallengeAttemptsExhausted);
        correctCodeAfterwards.Should().BeOfType<AuthenticationResult<LoginResult>.Failure>()
            .Which.Error.Reason.Should().Be(AuthenticationFailureReason.ChallengeAttemptsExhausted);
        (await store.GetAsync(challenge.Id, CancellationToken.None))!.Status
            .Should().Be(AuthenticationChallengeStatus.Exhausted);
        signIn.Verify(x => x.SignInAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task A_wrong_otp_code_records_the_unsuccessful_attempt()
    {
        TestDependencies dependencies = CreateDependencies();
        Mock<IAuthenticationSessionRegistry> registry = new();
        CompletePasswordLoginOtpHandler handler = dependencies.CreateHandler(new RecordingWriter([]), registry.Object);

        AuthenticationResult<LoginResult> result = await handler.Handle(
            new CompletePasswordLoginOtpCommand("challenge-42", "000000", "/home"),
            CancellationToken.None);

        result.Should().BeOfType<AuthenticationResult<LoginResult>.Failure>();
        registry.Verify(
            item => item.RecordFailedAttemptAsync("user-42", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Email_second_factor_completion_uses_email_subject_attempt_key_and_redacts_the_code()
    {
        AuthenticationChallenge emailChallenge = AuthenticationChallengePolicy.Default.CreatePasswordLoginEmailOtpChallenge(
            "user-42", "person@example.test", DateTimeOffset.UtcNow, false);
        AuthenticationUserDescriptor user = new("user-42", "user-42", null, "Given", "Family", "person@example.test", null, false, EmailConfirmed: true);
        TestDependencies dependencies = CreateDependencies(emailChallenge, user);
        List<SecurityAuditEvent> events = [];
        CompletePasswordLoginOtpHandler handler = dependencies.CreateHandler(new RecordingWriter(events));

        AuthenticationResult<LoginResult> result = await handler.Handle(
            new CompletePasswordLoginOtpCommand(emailChallenge.Id.Value, "123456", "/home"),
            CancellationToken.None);

        result.Should().BeOfType<AuthenticationResult<LoginResult>.Success>();
        dependencies.SignIn.Verify(x => x.SignInAsync("user-42", It.IsAny<CancellationToken>()), Times.Once);
        string emailAttemptKey = CreateKeys().Subject(AuthenticationMethod.Email, "user-42");
        dependencies.AttemptTracker.Verify(x => x.IsDeniedAsync(emailAttemptKey, It.IsAny<CancellationToken>()), Times.Once);
        events.Select(item => item.Outcome).Should().ContainInOrder(SecurityAuditOutcome.Started, SecurityAuditOutcome.Succeeded);
        events.Select(item => item.RedactedReason)
            .Where(reason => reason is not null)
            .Select(reason => reason!)
            .Should().NotContain("123456");
    }

    [Fact]
    public async Task Email_second_factor_completion_rejects_a_contact_that_was_changed_or_unconfirmed()
    {
        AuthenticationChallenge emailChallenge = AuthenticationChallengePolicy.Default.CreatePasswordLoginEmailOtpChallenge(
            "user-42", "old@example.test", DateTimeOffset.UtcNow, false);
        AuthenticationUserDescriptor changedUser = new("user-42", "user-42", null, "Given", "Family", "new@example.test", null, false, EmailConfirmed: true);
        TestDependencies dependencies = CreateDependencies(emailChallenge, changedUser);
        CompletePasswordLoginOtpHandler handler = dependencies.CreateHandler(new RecordingWriter([]));

        AuthenticationResult<LoginResult> result = await handler.Handle(
            new CompletePasswordLoginOtpCommand(emailChallenge.Id.Value, "123456", "/home"),
            CancellationToken.None);

        result.Should().BeOfType<AuthenticationResult<LoginResult>.Failure>()
            .Which.Error.Reason.Should().Be(AuthenticationFailureReason.NotAllowed);
        dependencies.Store.Verify(x => x.TryConsumeAsync(emailChallenge, CancellationToken.None), Times.Once);
        dependencies.Verifier.Verify(x => x.VerifyAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        dependencies.SignIn.Verify(x => x.SignInAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Wrong_email_second_factor_code_counts_against_the_email_subject_attempt_key()
    {
        AuthenticationChallenge emailChallenge = AuthenticationChallengePolicy.Default.CreatePasswordLoginEmailOtpChallenge(
            "user-42", "person@example.test", DateTimeOffset.UtcNow, false);
        AuthenticationUserDescriptor user = new("user-42", "user-42", null, "Given", "Family", "person@example.test", null, false, EmailConfirmed: true);
        TestDependencies dependencies = CreateDependencies(emailChallenge, user);
        dependencies.Verifier.Setup(x => x.VerifyAsync("user-42", emailChallenge.TokenPurpose, "987654", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        string emailAttemptKey = CreateKeys().Subject(AuthenticationMethod.Email, "user-42");
        dependencies.AttemptTracker.Setup(x => x.RegisterFailureAsync(emailAttemptKey, It.IsAny<int>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        Mock<IAuthenticationSessionRegistry> registry = new();
        List<SecurityAuditEvent> events = [];
        CompletePasswordLoginOtpHandler handler = dependencies.CreateHandler(new RecordingWriter(events), registry.Object);

        AuthenticationResult<LoginResult> result = await handler.Handle(
            new CompletePasswordLoginOtpCommand(emailChallenge.Id.Value, "987654", "/home"),
            CancellationToken.None);

        result.Should().BeOfType<AuthenticationResult<LoginResult>.Failure>()
            .Which.Error.Reason.Should().Be(AuthenticationFailureReason.ChallengeAttemptsExhausted);
        dependencies.AttemptTracker.Verify(x => x.RegisterFailureAsync(emailAttemptKey, It.IsAny<int>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Once);
        registry.Verify(x => x.RecordFailedAttemptAsync("user-42", It.IsAny<CancellationToken>()), Times.Once);
        JsonSerializer.Serialize(events).Should().NotContain("987654");
    }

    [Fact]
    public async Task Disabled_account_completion_is_denied_with_its_own_audit_reason()
    {
        TestDependencies dependencies = CreateDependencies();
        dependencies.SignIn
            .Setup(x => x.SignInAsync("user-42", It.IsAny<CancellationToken>()))
            .ReturnsAsync(AuthenticationSessionSignInResult.Failed(
                new AuthenticationFailure(AuthenticationFailureReason.AccountDisabled, "The account is disabled.")));
        List<SecurityAuditEvent> events = [];
        CompletePasswordLoginOtpHandler handler = dependencies.CreateHandler(new RecordingWriter(events));

        AuthenticationResult<LoginResult> result = await handler.Handle(Command(), CancellationToken.None);

        result.Should().BeOfType<AuthenticationResult<LoginResult>.Failure>()
            .Which.Error.Reason.Should().Be(AuthenticationFailureReason.AccountDisabled);
        events[^1].Outcome.Should().Be(SecurityAuditOutcome.Denied);
        events[^1].SubjectId.Should().Be("user-42");
        events[^1].RedactedReason.Should().Be(SecurityAuditReasonCatalog.AccountDisabled);
        events.Should().NotContain(audit => audit.RedactedReason == SecurityAuditReasonCatalog.AuthenticationFailed);
    }

    [Fact]
    public async Task No_access_completion_is_denied_with_its_own_audit_reason()
    {
        TestDependencies dependencies = CreateDependencies();
        dependencies.SignIn
            .Setup(x => x.SignInAsync("user-42", It.IsAny<CancellationToken>()))
            .ReturnsAsync(AuthenticationSessionSignInResult.Failed(
                new AuthenticationFailure(AuthenticationFailureReason.NoAccess, "The account has no access level.")));
        List<SecurityAuditEvent> events = [];
        CompletePasswordLoginOtpHandler handler = dependencies.CreateHandler(new RecordingWriter(events));

        AuthenticationResult<LoginResult> result = await handler.Handle(Command(), CancellationToken.None);

        result.Should().BeOfType<AuthenticationResult<LoginResult>.Failure>()
            .Which.Error.Reason.Should().Be(AuthenticationFailureReason.NoAccess);
        events[^1].Outcome.Should().Be(SecurityAuditOutcome.Denied);
        events[^1].SubjectId.Should().Be("user-42");
        events[^1].RedactedReason.Should().Be(SecurityAuditReasonCatalog.NoAccess);
        events.Should().NotContain(audit =>
            audit.RedactedReason == SecurityAuditReasonCatalog.AuthenticationFailed ||
            audit.RedactedReason == SecurityAuditReasonCatalog.AccountDisabled);
    }

    private static CompletePasswordLoginOtpCommand Command() => new("challenge-42", "123456", "/home");

    private static TestDependencies CreateDependencies(AuthenticationChallenge? configuredChallenge = null, AuthenticationUserDescriptor? user = null)
    {
        Mock<IAuthenticationChallengeStore> store = new();
        AuthenticationChallenge challenge = configuredChallenge ?? AuthenticationChallengePolicy.Default.CreatePasswordLoginOtpChallenge(
            "user-42", "+989121234567", DateTimeOffset.UtcNow, false);
        store.Setup(x => x.GetAsync(It.IsAny<AuthenticationChallengeId>(), It.IsAny<CancellationToken>())).ReturnsAsync(challenge);
        store.Setup(x => x.TryReserveAttemptAsync(It.IsAny<AuthenticationChallenge>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        store.Setup(x => x.TryConsumeAsync(It.IsAny<AuthenticationChallenge>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        Mock<IAuthenticationCodeVerifier> verifier = new();
        verifier.Setup(x => x.VerifyAsync("user-42", challenge.TokenPurpose, "123456", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        Mock<IAuthenticationSignInService> signIn = new();
        Mock<IAuthenticationRedirectResolver> redirects = new();
        redirects.Setup(x => x.ResolveAsync("/home", false, "user-42", It.IsAny<CancellationToken>())).ReturnsAsync("/home");
        Mock<IAuthenticationUserDirectory> userDirectory = new();
        userDirectory.Setup(x => x.FindByIdAsync("user-42", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user ?? new AuthenticationUserDescriptor("user-42", "user-42", null, "Given", "Family", null, "+989121234567", false, PhoneNumberConfirmed: true));
        Mock<IAuthenticationAttemptTracker> attemptTracker = new();
        attemptTracker.Setup(x => x.IsDeniedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        return new TestDependencies(store, verifier, signIn, redirects, userDirectory, attemptTracker);
    }

    private sealed record TestDependencies(
        Mock<IAuthenticationChallengeStore> Store,
        Mock<IAuthenticationCodeVerifier> Verifier,
        Mock<IAuthenticationSignInService> SignIn,
        Mock<IAuthenticationRedirectResolver> Redirects,
        Mock<IAuthenticationUserDirectory> UserDirectory,
        Mock<IAuthenticationAttemptTracker> AttemptTracker)
    {
        public CompletePasswordLoginOtpHandler CreateHandler(
            ISecurityAuditWriter writer,
            IAuthenticationSessionRegistry? registry = null) => new(
            Store.Object,
            Verifier.Object,
            SignIn.Object,
            Redirects.Object,
            writer,
            new TestRequestContext(),
            AttemptTracker.Object,
            new TestSecurityPolicyProvider(),
            CreateKeys(),
            TestSourceAttemptGuards.WithoutClientAddress(),
            new TestAuthenticationUtcClock(),
            registry ?? Mock.Of<IAuthenticationSessionRegistry>(),
            UserDirectory.Object);
    }

    private sealed class TestSecurityPolicyProvider : IAuthenticationSecurityPolicyProvider
    {
        public Task<SecurityAccessPolicy> GetAsync(CancellationToken cancellationToken) =>
            Task.FromResult(SecurityAccessPolicy.Default);
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
