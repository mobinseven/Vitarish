using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Application.Commands;
using Vitarish.Authentication.Application.Handlers;
using Vitarish.Authentication.Application.Results;
using Vitarish.Authentication.Domain.Challenges;
using Vitarish.Authentication.Domain.Methods;
using Vitarish.Authentication.Domain.Security;
using Vitarish.Authentication.Infrastructure;
using Vitarish.Authentication.Infrastructure.Options;
using Xunit;

namespace Vitarish.Authentication.Tests.Handlers;

public sealed class PasswordRecoveryHandlerTests
{
    [Theory]
    [InlineData(PasswordRecoveryChannel.Email)]
    [InlineData(PasswordRecoveryChannel.Sms)]
    public async Task Begin_returns_the_same_public_result_for_unknown_and_passwordless_contacts(PasswordRecoveryChannel channel)
    {
        BeginDependencies unknown = CreateBeginDependencies(channel);
        BeginDependencies passwordless = CreateBeginDependencies(channel);
        if (channel is PasswordRecoveryChannel.Email)
            passwordless.UserDirectory.Setup(x => x.FindByEmailAsync("contact", It.IsAny<CancellationToken>())).ReturnsAsync(User("user-42"));
        else
            passwordless.UserDirectory.Setup(x => x.FindByPhoneNumberAsync("contact", It.IsAny<CancellationToken>())).ReturnsAsync(User("user-42"));
        passwordless.AccountUpdater.Setup(x => x.UserHasPasswordAsync("user-42", It.IsAny<CancellationToken>())).ReturnsAsync(false);

        AuthenticationResult<ChallengeResult> unknownResult = await unknown.Handler.Handle(new(channel, "contact"), CancellationToken.None);
        AuthenticationResult<ChallengeResult> passwordlessResult = await passwordless.Handler.Handle(new(channel, "contact"), CancellationToken.None);

        ChallengeResult unknownChallenge = unknownResult.Should().BeOfType<AuthenticationResult<ChallengeResult>.Success>().Subject.Value;
        ChallengeResult passwordlessChallenge = passwordlessResult.Should().BeOfType<AuthenticationResult<ChallengeResult>.Success>().Subject.Value;
        unknownChallenge.Purpose.Should().Be(passwordlessChallenge.Purpose);
        unknownChallenge.RemainingAttempts.Should().Be(passwordlessChallenge.RemainingAttempts);
        unknown.UserDirectory.Verify(x => x.FindOrCreateByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        unknown.UserDirectory.Verify(x => x.FindOrCreateByPhoneNumberAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        unknown.EmailSender.Verify(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<AuthenticationChallengePurpose>(), It.IsAny<CancellationToken>()), Times.Never);
        unknown.PhoneSender.Verify(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<AuthenticationChallengePurpose>(), It.IsAny<CancellationToken>()), Times.Never);
        passwordless.EmailSender.Verify(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<AuthenticationChallengePurpose>(), It.IsAny<CancellationToken>()), Times.Never);
        passwordless.PhoneSender.Verify(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<AuthenticationChallengePurpose>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Begin_email_uses_registered_contact_even_when_email_sign_in_is_disabled()
    {
        BeginDependencies dependencies = CreateBeginDependencies(PasswordRecoveryChannel.Email);
        SecurityAccessPolicy disabledEmailLogin = SecurityAccessPolicy.ProductEvaluated with
        {
            EnabledAuthenticationMechanisms = new HashSet<AuthenticationMethod> { AuthenticationMethod.Password },
            PermittedPreAuthenticationActions = new HashSet<PreAuthenticationAction> { PreAuthenticationAction.PasswordLogin }
        };
        dependencies.SecurityPolicy.Setup(x => x.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(disabledEmailLogin);
        dependencies.UserDirectory.Setup(x => x.FindByEmailAsync("person@example.test", It.IsAny<CancellationToken>()))
            .ReturnsAsync(User("user-42", email: "Person@Example.test"));
        dependencies.AccountUpdater.Setup(x => x.UserHasPasswordAsync("user-42", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        dependencies.CodeGenerator.Setup(x => x.GenerateAsync("user-42", It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync("test-code");

        AuthenticationResult<ChallengeResult> result = await dependencies.Handler.Handle(
            new(PasswordRecoveryChannel.Email, "person@example.test"), CancellationToken.None);

        result.Should().BeOfType<AuthenticationResult<ChallengeResult>.Success>();
        dependencies.EmailSender.Verify(x => x.SendAsync("Person@Example.test", "test-code", AuthenticationChallengePurpose.EmailPasswordRecovery, It.IsAny<CancellationToken>()), Times.Once);
        dependencies.PhoneSender.Verify(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<AuthenticationChallengePurpose>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Begin_sms_uses_the_registered_phone_number()
    {
        BeginDependencies dependencies = CreateBeginDependencies(PasswordRecoveryChannel.Sms);
        dependencies.UserDirectory.Setup(x => x.FindByPhoneNumberAsync("+15550100", It.IsAny<CancellationToken>()))
            .ReturnsAsync(User("user-42", phone: "+15550101"));
        dependencies.AccountUpdater.Setup(x => x.UserHasPasswordAsync("user-42", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        dependencies.CodeGenerator.Setup(x => x.GenerateAsync("user-42", It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync("test-code");

        AuthenticationResult<ChallengeResult> result = await dependencies.Handler.Handle(
            new(PasswordRecoveryChannel.Sms, "+15550100"), CancellationToken.None);

        result.Should().BeOfType<AuthenticationResult<ChallengeResult>.Success>();
        dependencies.PhoneSender.Verify(x => x.SendAsync("+15550101", "test-code", AuthenticationChallengePurpose.PhonePasswordRecovery, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Begin_provider_failure_keeps_generic_success_and_records_redacted_detail()
    {
        BeginDependencies dependencies = CreateBeginDependencies(PasswordRecoveryChannel.Email);
        dependencies.UserDirectory.Setup(x => x.FindByEmailAsync("person@example.test", It.IsAny<CancellationToken>()))
            .ReturnsAsync(User("user-42", email: "person@example.test"));
        dependencies.AccountUpdater.Setup(x => x.UserHasPasswordAsync("user-42", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        dependencies.CodeGenerator.Setup(x => x.GenerateAsync("user-42", It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync("test-code");
        dependencies.EmailSender.Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<AuthenticationChallengePurpose>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("contains recipient and provider detail"));

        AuthenticationResult<ChallengeResult> result = await dependencies.Handler.Handle(
            new(PasswordRecoveryChannel.Email, "person@example.test"), CancellationToken.None);

        result.Should().BeOfType<AuthenticationResult<ChallengeResult>.Success>();
        dependencies.FailureRecorder.Verify(x => x.RecordAsync(PasswordRecoveryChannel.Email, nameof(IOException), It.IsAny<CancellationToken>()), Times.Once);
        dependencies.ChallengeStore.Verify(x => x.ReplaceAsync(
            It.Is<AuthenticationChallenge>(challenge => challenge.Status == AuthenticationChallengeStatus.Cancelled),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Complete_rejects_incorrect_code_and_does_not_reset_password()
    {
        CompleteDependencies dependencies = CreateCompleteDependencies(codeMatches: false);

        AuthenticationResult<bool> result = await dependencies.Handler.Handle(
            new(dependencies.Challenge.Id.Value, "wrong", "StrongPass1!"), CancellationToken.None);

        result.Should().BeOfType<AuthenticationResult<bool>.Failure>().Which.Error.Reason
            .Should().Be(AuthenticationFailureReason.InvalidVerificationCode);
        dependencies.AccountUpdater.Verify(x => x.ResetPasswordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        (await dependencies.ChallengeStore.GetAsync(dependencies.Challenge.Id, CancellationToken.None))!.FailedAttempts.Should().Be(1);
    }

    [Fact]
    public async Task Complete_rejects_expired_code_without_verification()
    {
        AuthenticationChallenge challenge = Challenge(DateTimeOffset.UtcNow.AddMinutes(-6));
        CompleteDependencies dependencies = CreateCompleteDependencies(challenge, codeMatches: true);

        AuthenticationResult<bool> result = await dependencies.Handler.Handle(
            new(challenge.Id.Value, "valid", "StrongPass1!"), CancellationToken.None);

        result.Should().BeOfType<AuthenticationResult<bool>.Failure>().Which.Error.Reason
            .Should().Be(AuthenticationFailureReason.ChallengeExpired);
        dependencies.CodeVerifier.Verify(x => x.VerifyAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Complete_rejects_replayed_code_and_changes_password_once()
    {
        CompleteDependencies dependencies = CreateCompleteDependencies(codeMatches: true);
        CompletePasswordRecoveryCommand command = new(dependencies.Challenge.Id.Value, "valid", "StrongPass1!");

        AuthenticationResult<bool> first = await dependencies.Handler.Handle(command, CancellationToken.None);
        AuthenticationResult<bool> replay = await dependencies.Handler.Handle(command, CancellationToken.None);

        first.Should().BeOfType<AuthenticationResult<bool>.Success>();
        replay.Should().BeOfType<AuthenticationResult<bool>.Failure>().Which.Error.Reason
            .Should().Be(AuthenticationFailureReason.ChallengeAlreadyUsed);
        dependencies.AccountUpdater.Verify(x => x.ResetPasswordAsync("user-42", "StrongPass1!", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Concurrent_completion_resets_password_only_once()
    {
        CompleteDependencies dependencies = CreateCompleteDependencies(codeMatches: true);
        CompletePasswordRecoveryCommand command = new(dependencies.Challenge.Id.Value, "valid", "StrongPass1!");

        AuthenticationResult<bool>[] results = await Task.WhenAll(
            dependencies.Handler.Handle(command, CancellationToken.None),
            dependencies.Handler.Handle(command, CancellationToken.None));

        results.OfType<AuthenticationResult<bool>.Success>().Should().ContainSingle();
        dependencies.AccountUpdater.Verify(x => x.ResetPasswordAsync("user-42", "StrongPass1!", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Complete_enforces_active_password_policy_and_leaves_code_available_for_retry()
    {
        CompleteDependencies dependencies = CreateCompleteDependencies(codeMatches: true);
        dependencies.SecurityPolicy.Setup(x => x.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(
            SecurityAccessPolicy.ProductEvaluated with { PasswordPolicy = PasswordPolicy.Default with { MinimumLength = 14 } });

        AuthenticationResult<bool> result = await dependencies.Handler.Handle(
            new(dependencies.Challenge.Id.Value, "valid", "StrongPass1!"), CancellationToken.None);

        result.Should().BeOfType<AuthenticationResult<bool>.Failure>().Which.Error.Reason.Should().Be(AuthenticationFailureReason.InvalidInput);
        (await dependencies.ChallengeStore.GetAsync(dependencies.Challenge.Id, CancellationToken.None))!.Status
            .Should().Be(AuthenticationChallengeStatus.Pending);
        dependencies.AccountUpdater.Verify(x => x.ResetPasswordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static BeginDependencies CreateBeginDependencies(PasswordRecoveryChannel channel)
    {
        Mock<IAuthenticationChallengeStore> store = new();
        Mock<IAuthenticationUserDirectory> userDirectory = new();
        Mock<IAuthenticationCodeGenerator> generator = new();
        Mock<IEmailAuthenticationCodeSender> emailSender = new();
        Mock<IPhoneAuthenticationCodeSender> phoneSender = new();
        Mock<IUserAccountUpdater> updater = new();
        Mock<IAuthenticationAttemptTracker> attempts = new();
        attempts.Setup(x => x.IsDeniedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        Mock<IAuthenticationRecoveryDeliveryFailureRecorder> recorder = new();
        recorder.Setup(x => x.RecordAsync(It.IsAny<PasswordRecoveryChannel>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        Mock<IAuthenticationSecurityPolicyProvider> policy = new();
        policy.Setup(x => x.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(SecurityAccessPolicy.ProductEvaluated);
        var handler = new BeginPasswordRecoveryHandler(store.Object, userDirectory.Object, generator.Object,
            emailSender.Object, phoneSender.Object, updater.Object, attempts.Object, CreateKeys(), recorder.Object,
            AuthenticationChallengePolicy.Default, policy.Object, TimeProvider.System);
        return new(handler, store, userDirectory, generator, emailSender, phoneSender, updater, recorder, policy);
    }

    private static CompleteDependencies CreateCompleteDependencies(bool codeMatches) =>
        CreateCompleteDependencies(Challenge(DateTimeOffset.UtcNow), codeMatches);

    private static CompleteDependencies CreateCompleteDependencies(AuthenticationChallenge challenge, bool codeMatches)
    {
        InMemoryAuthenticationChallengeStore store = new();
        store.SaveAsync(challenge, CancellationToken.None).GetAwaiter().GetResult();
        Mock<IAuthenticationCodeVerifier> verifier = new();
        verifier.Setup(x => x.VerifyAsync("user-42", challenge.TokenPurpose, "valid", It.IsAny<CancellationToken>())).ReturnsAsync(codeMatches);
        verifier.Setup(x => x.VerifyAsync("user-42", challenge.TokenPurpose, "wrong", It.IsAny<CancellationToken>())).ReturnsAsync(codeMatches);
        Mock<IUserAccountUpdater> updater = new();
        updater.Setup(x => x.ResetPasswordAsync("user-42", "StrongPass1!", It.IsAny<CancellationToken>()))
            .ReturnsAsync(PasswordChangeOutcome.Success());
        InMemoryAuthenticationAttemptTracker attemptTracker = new();
        Mock<IAuthenticationSecurityPolicyProvider> policy = new();
        policy.Setup(x => x.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(SecurityAccessPolicy.ProductEvaluated);
        CompletePasswordRecoveryHandler handler = new(store, verifier.Object, updater.Object, attemptTracker,
            CreateKeys(), policy.Object, TimeProvider.System);
        return new(handler, store, verifier, updater, policy, challenge);
    }

    private static AuthenticationChallenge Challenge(DateTimeOffset now) => AuthenticationChallengePolicy.Default
        .CreateEmailPasswordRecoveryChallenge("user-42", "person@example.test",
            AuthenticationChallengePolicy.Default.TokenPurpose(AuthenticationChallengePurpose.EmailPasswordRecovery), now);

    private static AuthenticationUserDescriptor User(string userId, string? email = "person@example.test", string? phone = "+15550101") =>
        new(userId, "person", null, null, null, email, phone, false);

    private static IAuthenticationAttemptKeyFactory CreateKeys() => new AuthenticationAttemptKeyFactory(
        Options.Create(new AuthenticationAttemptKeyOptions
        {
            Realm = "tests",
            CurrentPepper = "test-authentication-attempt-key-pepper-20260829"
        }));

    private sealed record BeginDependencies(
        BeginPasswordRecoveryHandler Handler,
        Mock<IAuthenticationChallengeStore> ChallengeStore,
        Mock<IAuthenticationUserDirectory> UserDirectory,
        Mock<IAuthenticationCodeGenerator> CodeGenerator,
        Mock<IEmailAuthenticationCodeSender> EmailSender,
        Mock<IPhoneAuthenticationCodeSender> PhoneSender,
        Mock<IUserAccountUpdater> AccountUpdater,
        Mock<IAuthenticationRecoveryDeliveryFailureRecorder> FailureRecorder,
        Mock<IAuthenticationSecurityPolicyProvider> SecurityPolicy);

    private sealed record CompleteDependencies(
        CompletePasswordRecoveryHandler Handler,
        InMemoryAuthenticationChallengeStore ChallengeStore,
        Mock<IAuthenticationCodeVerifier> CodeVerifier,
        Mock<IUserAccountUpdater> AccountUpdater,
        Mock<IAuthenticationSecurityPolicyProvider> SecurityPolicy,
        AuthenticationChallenge Challenge);
}
