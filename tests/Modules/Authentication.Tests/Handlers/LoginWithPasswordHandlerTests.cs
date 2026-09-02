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
using Vitarish.Authentication.Domain.Methods;
using Vitarish.Authentication.Domain.Security;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Requests.Domain.Entities;
using Vitarish.Authentication.Infrastructure;
using Vitarish.Authentication.Infrastructure.Options;
using Xunit;

namespace Vitarish.Authentication.Tests.Handlers;

public sealed class LoginWithPasswordHandlerTests
{
    [Fact]
    public async Task Product_profile_issues_phone_otp_only_for_two_factor_enabled_accounts()
    {
        TestDependencies dependencies = CreateDependencies(new PasswordSignInResult(
            PasswordSignInStatus.Succeeded,
            "user-42",
            RequiresProfileCompletion: false,
            RequiresTwoFactor: true));

        AuthenticationResult<LoginResult> result = await dependencies.Handler.Handle(
            new LoginWithPasswordCommand("user-42", "password", "/dashboard", true, "operation-42"),
            CancellationToken.None);

        result.Should().BeOfType<AuthenticationResult<LoginResult>.Failure>()
            .Which.Error.Reason.Should().Be(AuthenticationFailureReason.AdditionalStepRequired);
        dependencies.SignIn.Verify(x => x.CheckPasswordSignInAsync("user-42", "password", It.IsAny<CancellationToken>()), Times.Once);
        dependencies.SignIn.Verify(x => x.SignInAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
        dependencies.PhoneSender.Verify(x => x.SendAsync("+989121234567", "123456", AuthenticationChallengePurpose.PasswordLoginOtp, It.IsAny<CancellationToken>()), Times.Once);
        dependencies.ChallengeStore.Verify(x => x.SaveAsync(It.IsAny<AuthenticationChallenge>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Product_profile_signs_in_password_only_for_accounts_without_two_factor()
    {
        TestDependencies dependencies = CreateDependencies(new PasswordSignInResult(
            PasswordSignInStatus.Succeeded,
            "user-42",
            RequiresProfileCompletion: false,
            RequiresTwoFactor: false));

        AuthenticationResult<LoginResult> result = await dependencies.Handler.Handle(
            new LoginWithPasswordCommand("user-42", "password", "/dashboard", true, "operation-42"),
            CancellationToken.None);

        result.Should().BeOfType<AuthenticationResult<LoginResult>.Success>()
            .Which.Value.Should().Be(new LoginResult("user-42", "/dashboard", false));
        dependencies.SignIn.Verify(x => x.CheckPasswordSignInAsync("user-42", "password", It.IsAny<CancellationToken>()), Times.Once);
        dependencies.SignIn.Verify(x => x.SignInAsync("user-42", true, It.IsAny<CancellationToken>()), Times.Once);
        dependencies.PhoneSender.Verify(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<AuthenticationChallengePurpose>(), It.IsAny<CancellationToken>()), Times.Never);
        dependencies.ChallengeStore.Verify(x => x.SaveAsync(It.IsAny<AuthenticationChallenge>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Product_profile_starts_otp_when_identity_reports_requires_two_factor()
    {
        TestDependencies dependencies = CreateDependencies(new PasswordSignInResult(
            PasswordSignInStatus.RequiresTwoFactor,
            "user-42",
            RequiresProfileCompletion: false,
            RequiresTwoFactor: true));

        AuthenticationResult<LoginResult> result = await dependencies.Handler.Handle(
            new LoginWithPasswordCommand("user-42", "password", "/dashboard", false, "operation-42"),
            CancellationToken.None);

        result.Should().BeOfType<AuthenticationResult<LoginResult>.Failure>()
            .Which.Error.Reason.Should().Be(AuthenticationFailureReason.AdditionalStepRequired);
        dependencies.PhoneSender.Verify(x => x.SendAsync("+989121234567", "123456", AuthenticationChallengePurpose.PasswordLoginOtp, It.IsAny<CancellationToken>()), Times.Once);
        dependencies.SignIn.Verify(x => x.SignInAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Product_profile_does_not_issue_otp_when_phone_attempt_is_denied()
    {
        TestDependencies dependencies = CreateDependencies(new PasswordSignInResult(
            PasswordSignInStatus.Succeeded,
            "user-42",
            RequiresProfileCompletion: false,
            RequiresTwoFactor: true));
        string attemptKey = AuthenticationAttemptKeyFactoryForTests().Subject(AuthenticationMethod.Phone, "user-42");
        dependencies.AttemptTracker.Setup(x => x.IsDeniedAsync(attemptKey, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        AuthenticationResult<LoginResult> result = await dependencies.Handler.Handle(
            new LoginWithPasswordCommand("user-42", "password", "/dashboard", false, "operation-42"),
            CancellationToken.None);

        result.Should().BeOfType<AuthenticationResult<LoginResult>.Failure>()
            .Which.Error.Reason.Should().Be(AuthenticationFailureReason.LockedOut);
        dependencies.PhoneSender.Verify(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<AuthenticationChallengePurpose>(), It.IsAny<CancellationToken>()), Times.Never);
        dependencies.ChallengeStore.Verify(x => x.SaveAsync(It.IsAny<AuthenticationChallenge>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Completed_audit_failure_consumes_the_issued_otp_challenge()
    {
        TestDependencies dependencies = CreateDependencies(new PasswordSignInResult(
            PasswordSignInStatus.Succeeded,
            "user-42",
            RequiresProfileCompletion: false,
            RequiresTwoFactor: true));
        dependencies.AuditWriter.SetupSequence(x => x.WriteAsync(It.IsAny<SecurityAuditEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SecurityAuditWriteResult.Recorded)
            .ThrowsAsync(new SecurityAuditStoreException("offline", new InvalidOperationException()));

        AuthenticationResult<LoginResult> result = await dependencies.Handler.Handle(
            new LoginWithPasswordCommand("user-42", "password", "/dashboard", false, "operation-42"),
            CancellationToken.None);

        result.Should().BeOfType<AuthenticationResult<LoginResult>.Failure>()
            .Which.Error.Reason.Should().Be(AuthenticationFailureReason.ProviderFailure);
        dependencies.ChallengeStore.Verify(x => x.TryConsumeAsync(It.IsAny<AuthenticationChallenge>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Cancelled_phone_send_cancels_the_challenge_and_preserves_cancellation()
    {
        TestDependencies dependencies = CreateDependencies(new PasswordSignInResult(
            PasswordSignInStatus.Succeeded,
            "user-42",
            RequiresProfileCompletion: false,
            RequiresTwoFactor: true));
        dependencies.PhoneSender
            .Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<AuthenticationChallengePurpose>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        await Assert.ThrowsAsync<OperationCanceledException>(() => dependencies.Handler.Handle(
            new LoginWithPasswordCommand("user-42", "password", "/dashboard", false, "operation-42"),
            CancellationToken.None));

        dependencies.ChallengeStore.Verify(x => x.ReplaceAsync(
            It.Is<AuthenticationChallenge>(challenge => challenge.Status == AuthenticationChallengeStatus.Cancelled),
            CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task Disabled_password_login_is_audited_before_the_denial_is_returned()
    {
        SecurityAccessPolicy policy = SecurityAccessPolicy.ProductEvaluated with
        {
            PermittedPreAuthenticationActions = new HashSet<PreAuthenticationAction>
            {
                PreAuthenticationAction.AuthenticationProfile
            }
        };
        TestDependencies dependencies = CreateDependencies(
            new PasswordSignInResult(PasswordSignInStatus.Succeeded, "user-42", false, false),
            policy);

        AuthenticationResult<LoginResult> result = await dependencies.Handler.Handle(
            new LoginWithPasswordCommand("user-42", "password", "/dashboard", false, "operation-42"),
            CancellationToken.None);

        result.Should().BeOfType<AuthenticationResult<LoginResult>.Failure>()
            .Which.Error.Reason.Should().Be(AuthenticationFailureReason.NotAllowed);
        dependencies.AuditWriter.Verify(x => x.WriteAsync(
            It.Is<SecurityAuditEvent>(audit => audit.Outcome == SecurityAuditOutcome.Denied && audit.RedactedReason == "password_login_disabled"),
            It.IsAny<CancellationToken>()), Times.Once);
        dependencies.SignIn.VerifyNoOtherCalls();
    }

    private static TestDependencies CreateDependencies(PasswordSignInResult signInResult, SecurityAccessPolicy? policy = null)
    {
        Mock<IAuthenticationSignInService> signIn = new();
        signIn.Setup(x => x.CheckPasswordSignInAsync("user-42", "password", It.IsAny<CancellationToken>()))
            .ReturnsAsync(signInResult);

        Mock<IAuthenticationRedirectResolver> redirects = new();
        redirects.Setup(x => x.ResolveAsync("/dashboard", false, "user-42", It.IsAny<CancellationToken>()))
            .ReturnsAsync("/dashboard");

        Mock<IAuthenticationChallengeStore> challengeStore = new();
        challengeStore.Setup(x => x.GetAsync(It.IsAny<AuthenticationChallengeId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AuthenticationChallengePolicy.Default.CreatePasswordLoginOtpChallenge(
                "user-42", "+989121234567", "Authentication:PasswordLoginOtp", DateTimeOffset.UtcNow, false, false));
        Mock<IAuthenticationUserDirectory> userDirectory = new();
        userDirectory.Setup(x => x.FindByIdAsync("user-42", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthenticationUserDescriptor("user-42", "user-42", null, "Given", "Family", null, "+989121234567", false));

        Mock<IAuthenticationCodeGenerator> codeGenerator = new();
        codeGenerator.Setup(x => x.GenerateAsync("user-42", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("123456");

        Mock<IPhoneAuthenticationCodeSender> phoneSender = new();
        Mock<IAuthenticationAttemptTracker> attemptTracker = new();
        Mock<ISecurityAuditWriter> auditWriter = new();
        auditWriter.Setup(x => x.WriteAsync(It.IsAny<SecurityAuditEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SecurityAuditWriteResult.Recorded);

        LoginWithPasswordHandler handler = new(
            signIn.Object,
            redirects.Object,
            auditWriter.Object,
            new TestRequestContext(),
            AuthenticationProfilePolicy.ProductEvaluated,
            challengeStore.Object,
            AuthenticationChallengePolicy.Default,
            userDirectory.Object,
            codeGenerator.Object,
            phoneSender.Object,
            attemptTracker.Object,
            new TestSecurityPolicyProvider(policy ?? SecurityAccessPolicy.ProductEvaluated),
            AuthenticationAttemptKeyFactoryForTests());

        return new TestDependencies(handler, signIn, challengeStore, phoneSender, attemptTracker, auditWriter);
    }

    private sealed record TestDependencies(
        LoginWithPasswordHandler Handler,
        Mock<IAuthenticationSignInService> SignIn,
        Mock<IAuthenticationChallengeStore> ChallengeStore,
        Mock<IPhoneAuthenticationCodeSender> PhoneSender,
        Mock<IAuthenticationAttemptTracker> AttemptTracker,
        Mock<ISecurityAuditWriter> AuditWriter);

    private sealed class TestSecurityPolicyProvider(SecurityAccessPolicy policy) : IAuthenticationSecurityPolicyProvider
    {
        public Task<SecurityAccessPolicy> GetAsync(CancellationToken cancellationToken) =>
            Task.FromResult(policy);
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

    private static IAuthenticationAttemptKeyFactory AuthenticationAttemptKeyFactoryForTests() => new AuthenticationAttemptKeyFactory(
        Options.Create(new AuthenticationAttemptKeyOptions
        {
            Realm = "tests",
            CurrentPepper = "test-authentication-attempt-key-pepper-20260829"
        }));
}
