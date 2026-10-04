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
    public async Task Begin_applies_the_same_minimum_response_duration_to_unknown_passwordless_denied_and_eligible_contacts()
    {
        foreach (BeginScenario scenario in Enum.GetValues<BeginScenario>())
        {
            ManualTimeProvider clock = new(DateTimeOffset.UtcNow);
            BeginDependencies dependencies = CreateBeginDependencies(PasswordRecoveryChannel.Email, clock);
            if (scenario is BeginScenario.Passwordless or BeginScenario.Eligible)
            {
                dependencies.UserDirectory.Setup(x => x.FindByEmailAsync("contact", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(User("user-42"));
                dependencies.AccountUpdater.Setup(x => x.UserHasPasswordAsync("user-42", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(scenario is BeginScenario.Eligible);
            }

            if (scenario is BeginScenario.Denied)
                dependencies.AttemptTracker.Setup(x => x.IsDeniedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

            if (scenario is BeginScenario.Eligible)
            {
                dependencies.CodeGenerator.Setup(x => x.GenerateAsync("user-42", It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync("test-code");
                dependencies.EmailSender.Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<AuthenticationChallengePurpose>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);
            }

            Task<AuthenticationResult<ChallengeResult>> request = dependencies.Handler.Handle(
                new(PasswordRecoveryChannel.Email, "contact"), CancellationToken.None);

            await clock.TimerScheduled.Task.WaitAsync(TimeSpan.FromSeconds(5));
            request.IsCompleted.Should().BeFalse(scenario.ToString());
            await Task.Yield();
            clock.Advance(AuthenticationChallengePolicy.PasswordRecoveryMinimumResponseDuration);
            (await request).Should().BeOfType<AuthenticationResult<ChallengeResult>.Success>();
        }
    }

    [Fact]
    public async Task Begin_email_uses_registered_contact_even_when_email_sign_in_is_disabled()
    {
        BeginDependencies dependencies = CreateBeginDependencies(PasswordRecoveryChannel.Email);
        SecurityAccessPolicy disabledEmailLogin = SecurityAccessPolicy.ProductEvaluated with
        {
            EnabledAuthenticationMechanisms = new HashSet<AuthenticationMethod> { AuthenticationMethod.Password },
            PermittedPreAuthenticationActions = new HashSet<PreAuthenticationAction>
            {
                PreAuthenticationAction.PasswordLogin,
                PreAuthenticationAction.PasswordRecovery
            }
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
    public async Task Begin_sms_converts_a_local_format_number_before_lookup_and_delivery()
    {
        BeginDependencies dependencies = CreateBeginDependencies(PasswordRecoveryChannel.Sms);
        dependencies.UserDirectory.Setup(x => x.FindByPhoneNumberAsync("+989123456789", It.IsAny<CancellationToken>()))
            .ReturnsAsync(User("user-42", phone: "+989123456789"));
        dependencies.AccountUpdater.Setup(x => x.UserHasPasswordAsync("user-42", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        dependencies.CodeGenerator.Setup(x => x.GenerateAsync("user-42", It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync("test-code");

        AuthenticationResult<ChallengeResult> result = await dependencies.Handler.Handle(
            new(PasswordRecoveryChannel.Sms, "09123456789"), CancellationToken.None);

        result.Should().BeOfType<AuthenticationResult<ChallengeResult>.Success>();
        dependencies.UserDirectory.Verify(x => x.FindByPhoneNumberAsync("+989123456789", It.IsAny<CancellationToken>()), Times.Once);
        dependencies.PhoneSender.Verify(x => x.SendAsync("+989123456789", "test-code", AuthenticationChallengePurpose.PhonePasswordRecovery, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Begin_sms_returns_the_same_public_result_for_an_unconvertible_and_an_unknown_number()
    {
        BeginDependencies unconvertible = CreateBeginDependencies(PasswordRecoveryChannel.Sms);
        BeginDependencies unknown = CreateBeginDependencies(PasswordRecoveryChannel.Sms);

        AuthenticationResult<ChallengeResult> unconvertibleResult = await unconvertible.Handler.Handle(
            new(PasswordRecoveryChannel.Sms, "not-a-number"), CancellationToken.None);
        AuthenticationResult<ChallengeResult> unknownResult = await unknown.Handler.Handle(
            new(PasswordRecoveryChannel.Sms, "09123456789"), CancellationToken.None);

        ChallengeResult unconvertibleChallenge = unconvertibleResult.Should().BeOfType<AuthenticationResult<ChallengeResult>.Success>().Subject.Value;
        ChallengeResult unknownChallenge = unknownResult.Should().BeOfType<AuthenticationResult<ChallengeResult>.Success>().Subject.Value;
        unconvertibleChallenge.Purpose.Should().Be(unknownChallenge.Purpose);
        unconvertibleChallenge.RemainingAttempts.Should().Be(unknownChallenge.RemainingAttempts);
        unconvertible.UserDirectory.Verify(x => x.FindByPhoneNumberAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        unconvertible.PhoneSender.Verify(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<AuthenticationChallengePurpose>(), It.IsAny<CancellationToken>()), Times.Never);
        unknown.PhoneSender.Verify(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<AuthenticationChallengePurpose>(), It.IsAny<CancellationToken>()), Times.Never);
        unconvertible.FailureRecorder.Verify(x => x.RecordAsync(It.IsAny<PasswordRecoveryChannel>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        unknown.FailureRecorder.Verify(x => x.RecordAsync(It.IsAny<PasswordRecoveryChannel>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Begin_rejects_when_recovery_is_not_in_the_active_pre_authentication_allowlist()
    {
        BeginDependencies dependencies = CreateBeginDependencies(PasswordRecoveryChannel.Email);
        dependencies.SecurityPolicy.Setup(x => x.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(
            SecurityAccessPolicy.ProductEvaluated with
            {
                PermittedPreAuthenticationActions = new HashSet<PreAuthenticationAction> { PreAuthenticationAction.PasswordLogin }
            });

        AuthenticationResult<ChallengeResult> result = await dependencies.Handler.Handle(
            new(PasswordRecoveryChannel.Email, "person@example.test"), CancellationToken.None);

        result.Should().BeOfType<AuthenticationResult<ChallengeResult>.Failure>().Which.Error.Reason
            .Should().Be(AuthenticationFailureReason.NotAllowed);
        dependencies.UserDirectory.Verify(x => x.FindByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        dependencies.ChallengeStore.Verify(x => x.SaveAsync(It.IsAny<AuthenticationChallenge>(), It.IsAny<CancellationToken>()), Times.Never);
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
    public async Task Complete_releases_the_verified_code_after_a_known_password_update_failure()
    {
        CompleteDependencies dependencies = CreateCompleteDependencies(codeMatches: true);
        dependencies.AccountUpdater.SetupSequence(x => x.ResetPasswordAsync("user-42", "StrongPass1!", CancellationToken.None))
            .ReturnsAsync(PasswordChangeOutcome.Failure(PasswordChangeStatus.Failed))
            .ReturnsAsync(PasswordChangeOutcome.Success());
        CompletePasswordRecoveryCommand command = new(dependencies.Challenge.Id.Value, "valid", "StrongPass1!");

        AuthenticationResult<bool> failed = await dependencies.Handler.Handle(command, CancellationToken.None);
        AuthenticationResult<bool> retried = await dependencies.Handler.Handle(command, CancellationToken.None);

        failed.Should().BeOfType<AuthenticationResult<bool>.Failure>().Which.Error.Reason
            .Should().Be(AuthenticationFailureReason.ProviderFailure);
        (await dependencies.ChallengeStore.GetAsync(dependencies.Challenge.Id, CancellationToken.None))!.Status
            .Should().Be(AuthenticationChallengeStatus.Completed);
        retried.Should().BeOfType<AuthenticationResult<bool>.Success>();
        dependencies.AccountUpdater.Verify(x => x.ResetPasswordAsync("user-42", "StrongPass1!", CancellationToken.None), Times.Exactly(2));
    }

    [Fact]
    public async Task Complete_releases_the_verified_code_after_a_user_specific_password_policy_rejection()
    {
        CompleteDependencies dependencies = CreateCompleteDependencies(codeMatches: true);
        dependencies.AccountUpdater.SetupSequence(x => x.ResetPasswordAsync("user-42", "StrongPass1!", CancellationToken.None))
            .ReturnsAsync(PasswordChangeOutcome.PolicyFailure([PasswordPolicyViolation.MinimumLength]))
            .ReturnsAsync(PasswordChangeOutcome.Success());
        CompletePasswordRecoveryCommand command = new(dependencies.Challenge.Id.Value, "valid", "StrongPass1!");

        AuthenticationResult<bool> rejected = await dependencies.Handler.Handle(command, CancellationToken.None);
        AuthenticationResult<bool> retried = await dependencies.Handler.Handle(command, CancellationToken.None);

        rejected.Should().BeOfType<AuthenticationResult<bool>.Failure>().Which.Error.Reason
            .Should().Be(AuthenticationFailureReason.InvalidInput);
        retried.Should().BeOfType<AuthenticationResult<bool>.Success>();
        dependencies.AccountUpdater.Verify(x => x.ResetPasswordAsync("user-42", "StrongPass1!", CancellationToken.None), Times.Exactly(2));
    }

    [Fact]
    public async Task Complete_finishes_the_password_change_if_request_cancellation_arrives_after_mutation_starts()
    {
        CompleteDependencies dependencies = CreateCompleteDependencies(codeMatches: true);
        using CancellationTokenSource cancellation = new();
        dependencies.AccountUpdater.Setup(x => x.ResetPasswordAsync("user-42", "StrongPass1!", CancellationToken.None))
            .Callback(() => cancellation.Cancel())
            .ReturnsAsync(PasswordChangeOutcome.Success());

        AuthenticationResult<bool> result = await dependencies.Handler.Handle(
            new(dependencies.Challenge.Id.Value, "valid", "StrongPass1!"), cancellation.Token);

        result.Should().BeOfType<AuthenticationResult<bool>.Success>();
        (await dependencies.ChallengeStore.GetAsync(dependencies.Challenge.Id, CancellationToken.None))!.Status
            .Should().Be(AuthenticationChallengeStatus.Completed);
        dependencies.AccountUpdater.Verify(x => x.ResetPasswordAsync("user-42", "StrongPass1!", CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task Complete_reconciles_an_ambiguous_password_update_exception_before_consuming_the_code()
    {
        CompleteDependencies dependencies = CreateCompleteDependencies(codeMatches: true);
        dependencies.AccountUpdater.Setup(x => x.ResetPasswordAsync("user-42", "StrongPass1!", CancellationToken.None))
            .ThrowsAsync(new IOException("provider response was lost"));
        dependencies.AccountUpdater.Setup(x => x.PasswordMatchesAsync("user-42", "StrongPass1!", CancellationToken.None))
            .ReturnsAsync(true);

        AuthenticationResult<bool> result = await dependencies.Handler.Handle(
            new(dependencies.Challenge.Id.Value, "valid", "StrongPass1!"), CancellationToken.None);

        result.Should().BeOfType<AuthenticationResult<bool>.Success>();
        (await dependencies.ChallengeStore.GetAsync(dependencies.Challenge.Id, CancellationToken.None))!.Status
            .Should().Be(AuthenticationChallengeStatus.Completed);
    }

    [Fact]
    public async Task Complete_releases_the_code_if_an_ambiguous_password_update_is_proven_not_applied()
    {
        CompleteDependencies dependencies = CreateCompleteDependencies(codeMatches: true);
        dependencies.AccountUpdater.Setup(x => x.ResetPasswordAsync("user-42", "StrongPass1!", CancellationToken.None))
            .ThrowsAsync(new IOException("provider failed before applying the reset"));
        dependencies.AccountUpdater.Setup(x => x.PasswordMatchesAsync("user-42", "StrongPass1!", CancellationToken.None))
            .ReturnsAsync(false);

        await Assert.ThrowsAsync<IOException>(() => dependencies.Handler.Handle(
            new(dependencies.Challenge.Id.Value, "valid", "StrongPass1!"), CancellationToken.None));

        (await dependencies.ChallengeStore.GetAsync(dependencies.Challenge.Id, CancellationToken.None))!.Status
            .Should().Be(AuthenticationChallengeStatus.Pending);
        dependencies.AccountUpdater.Setup(x => x.ResetPasswordAsync("user-42", "StrongPass1!", CancellationToken.None))
            .ReturnsAsync(PasswordChangeOutcome.Success());
        AuthenticationResult<bool> retry = await dependencies.Handler.Handle(
            new(dependencies.Challenge.Id.Value, "valid", "StrongPass1!"), CancellationToken.None);
        retry.Should().BeOfType<AuthenticationResult<bool>.Success>();
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

    [Fact]
    public async Task Complete_rejects_when_recovery_is_not_in_the_active_pre_authentication_allowlist()
    {
        Mock<IAuthenticationChallengeStore> store = new();
        Mock<IAuthenticationCodeVerifier> verifier = new();
        Mock<IUserAccountUpdater> updater = new();
        Mock<IAuthenticationSecurityPolicyProvider> policy = new();
        policy.Setup(x => x.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(
            SecurityAccessPolicy.ProductEvaluated with
            {
                PermittedPreAuthenticationActions = new HashSet<PreAuthenticationAction> { PreAuthenticationAction.PasswordLogin }
            });
        CompletePasswordRecoveryHandler handler = new(store.Object, verifier.Object, updater.Object,
            new InMemoryAuthenticationAttemptTracker(), CreateKeys(), policy.Object, TimeProvider.System);

        AuthenticationResult<bool> result = await handler.Handle(
            new("challenge-id", "code", "StrongPass1!"), CancellationToken.None);

        result.Should().BeOfType<AuthenticationResult<bool>.Failure>().Which.Error.Reason
            .Should().Be(AuthenticationFailureReason.NotAllowed);
        store.Verify(x => x.GetAsync(It.IsAny<AuthenticationChallengeId>(), It.IsAny<CancellationToken>()), Times.Never);
        verifier.Verify(x => x.VerifyAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        updater.Verify(x => x.ResetPasswordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static BeginDependencies CreateBeginDependencies(PasswordRecoveryChannel channel, TimeProvider? clock = null)
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
            AuthenticationChallengePolicy.Default, policy.Object, clock ?? TimeProvider.System);
        return new(handler, store, userDirectory, generator, emailSender, phoneSender, updater, attempts, recorder, policy);
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
        updater.Setup(x => x.ResetPasswordAsync("user-42", "StrongPass1!", CancellationToken.None))
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
        Mock<IAuthenticationAttemptTracker> AttemptTracker,
        Mock<IAuthenticationRecoveryDeliveryFailureRecorder> FailureRecorder,
        Mock<IAuthenticationSecurityPolicyProvider> SecurityPolicy);

    private sealed record CompleteDependencies(
        CompletePasswordRecoveryHandler Handler,
        InMemoryAuthenticationChallengeStore ChallengeStore,
        Mock<IAuthenticationCodeVerifier> CodeVerifier,
        Mock<IUserAccountUpdater> AccountUpdater,
        Mock<IAuthenticationSecurityPolicyProvider> SecurityPolicy,
        AuthenticationChallenge Challenge);

    private enum BeginScenario
    {
        Unknown,
        Passwordless,
        Denied,
        Eligible
    }

    private sealed class ManualTimeProvider(DateTimeOffset initial) : TimeProvider
    {
        private readonly object sync = new();
        private readonly List<ManualTimer> timers = [];
        private DateTimeOffset now = initial;
        private long timestamp;

        public TaskCompletionSource TimerScheduled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override DateTimeOffset GetUtcNow()
        {
            lock (sync)
                return now;
        }

        public override long GetTimestamp()
        {
            lock (sync)
                return timestamp;
        }

        public override long TimestampFrequency => TimeSpan.TicksPerSecond;

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            ManualTimer timer = new(this, callback, state);
            lock (sync)
            {
                timers.Add(timer);
                timer.Change(dueTime, period);
            }

            TimerScheduled.TrySetResult();
            return timer;
        }

        public void Advance(TimeSpan duration)
        {
            ManualTimer[] due;
            lock (sync)
            {
                now = now.Add(duration);
                timestamp = checked(timestamp + duration.Ticks);
                due = timers.Where(timer => timer.IsDue(now)).ToArray();
            }

            foreach (ManualTimer timer in due)
                timer.Fire();
        }

        private sealed class ManualTimer(ManualTimeProvider owner, TimerCallback callback, object? state) : ITimer
        {
            private DateTimeOffset? dueAt;
            private TimeSpan period = Timeout.InfiniteTimeSpan;
            private bool disposed;

            public bool Change(TimeSpan dueTime, TimeSpan timerPeriod)
            {
                lock (owner.sync)
                {
                    if (disposed)
                        return false;

                    dueAt = dueTime == Timeout.InfiniteTimeSpan ? null : owner.now.Add(dueTime);
                    period = timerPeriod;
                    return true;
                }
            }

            public bool IsDue(DateTimeOffset current) => !disposed && dueAt is DateTimeOffset due && due <= current;

            public void Fire()
            {
                lock (owner.sync)
                {
                    if (disposed || dueAt is null)
                        return;
                    dueAt = period == Timeout.InfiniteTimeSpan ? null : owner.now.Add(period);
                }

                callback(state);
            }

            public void Dispose()
            {
                lock (owner.sync)
                {
                    disposed = true;
                    dueAt = null;
                }
            }

            public ValueTask DisposeAsync()
            {
                Dispose();
                return ValueTask.CompletedTask;
            }
        }
    }
}
