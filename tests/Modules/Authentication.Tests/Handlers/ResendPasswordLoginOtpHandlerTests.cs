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
using Vitarish.Authentication.Application.Security;
using Vitarish.Authentication.Domain.Challenges;
using Vitarish.Authentication.Domain.Methods;
using Vitarish.Authentication.Domain.Security;
using Vitarish.Authentication.Infrastructure;
using Vitarish.Authentication.Infrastructure.Options;
using Vitarish.Authentication.Tests.Security;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Requests.Domain.Entities;
using Xunit;

namespace Vitarish.Authentication.Tests.Handlers;

public sealed class ResendPasswordLoginOtpHandlerTests
{
    [Fact]
    public async Task Email_resend_uses_the_stored_confirmed_destination_and_preserves_the_challenge()
    {
        AuthenticationChallenge challenge = AuthenticationChallengePolicy.Default.CreatePasswordLoginEmailOtpChallenge(
            "user-42", "person@example.test", TestClock.UtcNow.AddMinutes(-1), false);
        TestDependencies dependencies = CreateDependencies(
            challenge,
            new AuthenticationUserDescriptor("user-42", "user-42", null, "Given", "Family", "person@example.test", null, false, EmailConfirmed: true));

        AuthenticationResult<ChallengeResult> result = await dependencies.Handler.Handle(
            new ResendPasswordLoginOtpCommand(challenge.Id.Value),
            CancellationToken.None);

        ChallengeResult resent = result.Should().BeOfType<AuthenticationResult<ChallengeResult>.Success>().Which.Value;
        resent.Should().Be(new ChallengeResult(challenge.Id.Value, challenge.Purpose, challenge.ExpiresAt, 1));
        dependencies.CodeGenerator.Verify(x => x.GenerateAsync("user-42", challenge.TokenPurpose, It.IsAny<CancellationToken>()), Times.Once);
        dependencies.EmailSender.Verify(x => x.SendAsync("person@example.test", "123456", AuthenticationChallengePurpose.PasswordLoginEmailOtp, It.IsAny<CancellationToken>()), Times.Once);
        dependencies.PhoneSender.VerifyNoOtherCalls();
        dependencies.AttemptTracker.Verify(x => x.IsDeniedAsync(dependencies.EmailSubjectKey, It.IsAny<CancellationToken>()), Times.Once);
        dependencies.Store.Verify(x => x.TryReserveAttemptAsync(challenge, It.IsAny<CancellationToken>()), Times.Once);
        dependencies.Store.Verify(x => x.ReleaseAttemptAsync(challenge, CancellationToken.None), Times.Once);
        dependencies.AuditEvents.Select(item => item.Outcome).Should().ContainInOrder(SecurityAuditOutcome.Started, SecurityAuditOutcome.Succeeded);
        dependencies.AuditEvents.Select(item => item.RedactedReason)
            .Where(reason => reason is not null)
            .Select(reason => reason!)
            .Should().NotContain("123456");
    }

    [Fact]
    public async Task Phone_resend_keeps_the_phone_channel_and_requires_the_stored_phone_to_remain_confirmed()
    {
        AuthenticationChallenge challenge = AuthenticationChallengePolicy.Default.CreatePasswordLoginOtpChallenge(
            "user-42", "+989121234567", TestClock.UtcNow.AddMinutes(-1), false);
        TestDependencies dependencies = CreateDependencies(
            challenge,
            new AuthenticationUserDescriptor("user-42", "user-42", null, "Given", "Family", null, "+989121234567", false, PhoneNumberConfirmed: true));

        AuthenticationResult<ChallengeResult> result = await dependencies.Handler.Handle(
            new ResendPasswordLoginOtpCommand(challenge.Id.Value),
            CancellationToken.None);

        result.Should().BeOfType<AuthenticationResult<ChallengeResult>.Success>();
        dependencies.PhoneSender.Verify(x => x.SendAsync("+989121234567", "123456", AuthenticationChallengePurpose.PasswordLoginOtp, It.IsAny<CancellationToken>()), Times.Once);
        dependencies.EmailSender.VerifyNoOtherCalls();
        dependencies.AttemptTracker.Verify(x => x.IsDeniedAsync(dependencies.PhoneSubjectKey, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(false, "+989121234567")]
    [InlineData(true, "+989129876543")]
    public async Task Phone_resend_rejects_removed_or_unconfirmed_contacts(bool phoneConfirmed, string currentPhone)
    {
        AuthenticationChallenge challenge = AuthenticationChallengePolicy.Default.CreatePasswordLoginOtpChallenge(
            "user-42", "+989121234567", TestClock.UtcNow.AddMinutes(-1), false);
        TestDependencies dependencies = CreateDependencies(
            challenge,
            new AuthenticationUserDescriptor("user-42", "user-42", null, "Given", "Family", null, currentPhone, false, PhoneNumberConfirmed: phoneConfirmed));

        AuthenticationResult<ChallengeResult> result = await dependencies.Handler.Handle(
            new ResendPasswordLoginOtpCommand(challenge.Id.Value),
            CancellationToken.None);

        result.Should().BeOfType<AuthenticationResult<ChallengeResult>.Failure>()
            .Which.Error.Reason.Should().Be(AuthenticationFailureReason.NotAllowed);
        dependencies.Store.Verify(x => x.TryConsumeAsync(challenge, CancellationToken.None), Times.Once);
        dependencies.PhoneSender.VerifyNoOtherCalls();
        dependencies.CodeGenerator.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Resend_rejects_a_missing_challenge_without_disclosing_it_or_sending_a_code()
    {
        TestDependencies dependencies = CreateDependencies(challenge: null, user: null);

        AuthenticationResult<ChallengeResult> result = await dependencies.Handler.Handle(
            new ResendPasswordLoginOtpCommand("nonexistent-challenge"),
            CancellationToken.None);

        result.Should().BeOfType<AuthenticationResult<ChallengeResult>.Failure>()
            .Which.Error.Reason.Should().Be(AuthenticationFailureReason.InvalidCredentials);
        dependencies.EmailSender.VerifyNoOtherCalls();
        dependencies.PhoneSender.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Resend_rejects_an_email_factor_when_its_policy_action_is_disallowed()
    {
        AuthenticationChallenge challenge = AuthenticationChallengePolicy.Default.CreatePasswordLoginEmailOtpChallenge(
            "user-42", "person@example.test", TestClock.UtcNow.AddMinutes(-1), false);
        SecurityAccessPolicy policy = SecurityAccessPolicy.Default with
        {
            PermittedPreAuthenticationActions = SecurityAccessPolicy.Default.PermittedPreAuthenticationActions
                .Where(action => action is not PreAuthenticationAction.PasswordPlusEmailOtp)
                .ToHashSet()
        };
        TestDependencies dependencies = CreateDependencies(
            challenge,
            new AuthenticationUserDescriptor("user-42", "user-42", null, "Given", "Family", "person@example.test", null, false, EmailConfirmed: true),
            policy);

        AuthenticationResult<ChallengeResult> result = await dependencies.Handler.Handle(
            new ResendPasswordLoginOtpCommand(challenge.Id.Value),
            CancellationToken.None);

        result.Should().BeOfType<AuthenticationResult<ChallengeResult>.Failure>()
            .Which.Error.Reason.Should().Be(AuthenticationFailureReason.NotAllowed);
        dependencies.EmailSender.VerifyNoOtherCalls();
        dependencies.CodeGenerator.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(false, "person@example.test")]
    [InlineData(true, "changed@example.test")]
    public async Task Resend_rejects_removed_or_unconfirmed_email_contacts(bool emailConfirmed, string currentEmail)
    {
        AuthenticationChallenge challenge = AuthenticationChallengePolicy.Default.CreatePasswordLoginEmailOtpChallenge(
            "user-42", "person@example.test", TestClock.UtcNow.AddMinutes(-1), false);
        TestDependencies dependencies = CreateDependencies(
            challenge,
            new AuthenticationUserDescriptor("user-42", "user-42", null, "Given", "Family", currentEmail, null, false, EmailConfirmed: emailConfirmed));

        AuthenticationResult<ChallengeResult> result = await dependencies.Handler.Handle(
            new ResendPasswordLoginOtpCommand(challenge.Id.Value),
            CancellationToken.None);

        result.Should().BeOfType<AuthenticationResult<ChallengeResult>.Failure>()
            .Which.Error.Reason.Should().Be(AuthenticationFailureReason.NotAllowed);
        dependencies.Store.Verify(x => x.TryConsumeAsync(challenge, CancellationToken.None), Times.Once);
        dependencies.EmailSender.VerifyNoOtherCalls();
        dependencies.CodeGenerator.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Resend_refuses_expired_challenges()
    {
        AuthenticationChallenge challenge = AuthenticationChallengePolicy.Default.CreatePasswordLoginEmailOtpChallenge(
            "user-42", "person@example.test", TestClock.UtcNow.AddMinutes(-10), false);
        TestDependencies dependencies = CreateDependencies(
            challenge,
            new AuthenticationUserDescriptor("user-42", "user-42", null, "Given", "Family", "person@example.test", null, false, EmailConfirmed: true));

        AuthenticationResult<ChallengeResult> result = await dependencies.Handler.Handle(
            new ResendPasswordLoginOtpCommand(challenge.Id.Value),
            CancellationToken.None);

        result.Should().BeOfType<AuthenticationResult<ChallengeResult>.Failure>()
            .Which.Error.Reason.Should().Be(AuthenticationFailureReason.ChallengeExpired);
        dependencies.EmailSender.VerifyNoOtherCalls();
        dependencies.CodeGenerator.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Resend_refuses_exhausted_challenges()
    {
        AuthenticationChallenge challenge = AuthenticationChallengePolicy.Default.CreatePasswordLoginEmailOtpChallenge(
            "user-42", "person@example.test", TestClock.UtcNow.AddMinutes(-1), false);
        challenge.RegisterFailure(TestClock.UtcNow);
        TestDependencies dependencies = CreateDependencies(
            challenge,
            new AuthenticationUserDescriptor("user-42", "user-42", null, "Given", "Family", "person@example.test", null, false, EmailConfirmed: true));

        AuthenticationResult<ChallengeResult> result = await dependencies.Handler.Handle(
            new ResendPasswordLoginOtpCommand(challenge.Id.Value),
            CancellationToken.None);

        result.Should().BeOfType<AuthenticationResult<ChallengeResult>.Failure>()
            .Which.Error.Reason.Should().Be(AuthenticationFailureReason.ChallengeAttemptsExhausted);
        dependencies.EmailSender.VerifyNoOtherCalls();
        dependencies.CodeGenerator.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Resend_checks_email_subject_lockout_before_generating_or_delivering_a_code()
    {
        AuthenticationChallenge challenge = AuthenticationChallengePolicy.Default.CreatePasswordLoginEmailOtpChallenge(
            "user-42", "person@example.test", TestClock.UtcNow.AddMinutes(-1), false);
        TestDependencies dependencies = CreateDependencies(
            challenge,
            new AuthenticationUserDescriptor("user-42", "user-42", null, "Given", "Family", "person@example.test", null, false, EmailConfirmed: true));
        dependencies.AttemptTracker.Setup(x => x.IsDeniedAsync(dependencies.EmailSubjectKey, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        AuthenticationResult<ChallengeResult> result = await dependencies.Handler.Handle(
            new ResendPasswordLoginOtpCommand(challenge.Id.Value),
            CancellationToken.None);

        result.Should().BeOfType<AuthenticationResult<ChallengeResult>.Failure>()
            .Which.Error.Reason.Should().Be(AuthenticationFailureReason.LockedOut);
        dependencies.AttemptTracker.Verify(x => x.IsDeniedAsync(dependencies.EmailSubjectKey, It.IsAny<CancellationToken>()), Times.Once);
        dependencies.CodeGenerator.VerifyNoOtherCalls();
        dependencies.EmailSender.VerifyNoOtherCalls();
    }

    private static TestDependencies CreateDependencies(
        AuthenticationChallenge? challenge,
        AuthenticationUserDescriptor? user,
        SecurityAccessPolicy? policy = null)
    {
        Mock<IAuthenticationChallengeStore> store = new();
        store.Setup(x => x.GetAsync(It.IsAny<AuthenticationChallengeId>(), It.IsAny<CancellationToken>())).ReturnsAsync(challenge);
        store.Setup(x => x.TryReserveAttemptAsync(It.IsAny<AuthenticationChallenge>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        Mock<IAuthenticationUserDirectory> userDirectory = new();
        userDirectory.Setup(x => x.FindByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(user);
        Mock<IAuthenticationCodeGenerator> codeGenerator = new();
        codeGenerator.Setup(x => x.GenerateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync("123456");
        Mock<IPhoneAuthenticationCodeSender> phoneSender = new();
        Mock<IEmailAuthenticationCodeSender> emailSender = new();
        Mock<IAuthenticationAttemptTracker> attemptTracker = new();
        attemptTracker.Setup(x => x.IsDeniedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        Mock<ISecurityAuditWriter> auditWriter = new();
        List<SecurityAuditEvent> auditEvents = [];
        auditWriter.Setup(x => x.WriteAsync(It.IsAny<SecurityAuditEvent>(), It.IsAny<CancellationToken>()))
            .Callback<SecurityAuditEvent, CancellationToken>((audit, _) => auditEvents.Add(audit))
            .ReturnsAsync(SecurityAuditWriteResult.Recorded);

        AuthenticationAttemptKeyFactory keys = new(Options.Create(new AuthenticationAttemptKeyOptions
        {
            Realm = "tests",
            CurrentPepper = "test-authentication-attempt-key-pepper-20260829"
        }));
        ResendPasswordLoginOtpHandler handler = new(
            store.Object,
            userDirectory.Object,
            codeGenerator.Object,
            phoneSender.Object,
            emailSender.Object,
            attemptTracker.Object,
            new TestSecurityPolicyProvider(policy ?? SecurityAccessPolicy.Default),
            keys,
            TestSourceAttemptGuards.WithoutClientAddress(),
            auditWriter.Object,
            new TestRequestContext(),
            new TestClock());

        return new TestDependencies(handler, store, userDirectory, codeGenerator, phoneSender, emailSender, attemptTracker, auditEvents,
            keys.Subject(AuthenticationMethod.Email, "user-42"), keys.Subject(AuthenticationMethod.Phone, "user-42"));
    }

    private sealed record TestDependencies(
        ResendPasswordLoginOtpHandler Handler,
        Mock<IAuthenticationChallengeStore> Store,
        Mock<IAuthenticationUserDirectory> UserDirectory,
        Mock<IAuthenticationCodeGenerator> CodeGenerator,
        Mock<IPhoneAuthenticationCodeSender> PhoneSender,
        Mock<IEmailAuthenticationCodeSender> EmailSender,
        Mock<IAuthenticationAttemptTracker> AttemptTracker,
        List<SecurityAuditEvent> AuditEvents,
        string EmailSubjectKey,
        string PhoneSubjectKey);

    private sealed class TestSecurityPolicyProvider(SecurityAccessPolicy policy) : IAuthenticationSecurityPolicyProvider
    {
        public Task<SecurityAccessPolicy> GetAsync(CancellationToken cancellationToken) => Task.FromResult(policy);
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

    private sealed class TestClock : IAuthenticationUtcClock
    {
        public static DateTime UtcNow { get; } = new(2026, 11, 6, 8, 0, 0, DateTimeKind.Utc);
        DateTime IAuthenticationUtcClock.UtcNow => UtcNow;
    }
}
