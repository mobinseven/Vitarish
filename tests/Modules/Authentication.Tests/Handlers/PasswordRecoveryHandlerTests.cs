using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
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
using Vitarish.Authentication.Tests.Security;

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
        unknown.Dispatcher!.Verify(x => x.DispatchAsync(It.IsAny<AuthenticationChallenge>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        passwordless.Dispatcher!.Verify(x => x.DispatchAsync(It.IsAny<AuthenticationChallenge>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Begin_applies_the_same_minimum_response_duration_to_unknown_passwordless_denied_and_eligible_contacts()
    {
        foreach (BeginScenario scenario in Enum.GetValues<BeginScenario>())
        {
            FakeTimeProvider clock = new(DateTimeOffset.UtcNow);
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
            }

            Task<AuthenticationResult<ChallengeResult>> request = dependencies.Handler.Handle(
                new(PasswordRecoveryChannel.Email, "contact"), CancellationToken.None);

            request.IsCompleted.Should().BeFalse(scenario.ToString());
            clock.Advance(AuthenticationChallengePolicy.PasswordRecoveryMinimumResponseDuration);
            (await request).Should().BeOfType<AuthenticationResult<ChallengeResult>.Success>();
            dependencies.Dispatcher!.Verify(
                x => x.DispatchAsync(It.IsAny<AuthenticationChallenge>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
                scenario is BeginScenario.Eligible ? Times.Once() : Times.Never());
        }
    }

    [Fact]
    public async Task Begin_slow_delivery_keeps_eligible_and_ineligible_requests_at_the_padded_duration()
    {
        FakeTimeProvider clock = new(DateTimeOffset.UtcNow);
        TaskCompletionSource deliveryStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using BeginDependencies eligible = CreateBeginDependencies(PasswordRecoveryChannel.Email, clock, useRealDispatcher: true);
        eligible.UserDirectory.Setup(x => x.FindByEmailAsync("person@example.test", It.IsAny<CancellationToken>()))
            .ReturnsAsync(User("user-42", email: "person@example.test"));
        eligible.AccountUpdater.Setup(x => x.UserHasPasswordAsync("user-42", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        eligible.CodeGenerator.Setup(x => x.GenerateAsync("user-42", It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync("test-code");
        eligible.EmailSender.Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<AuthenticationChallengePurpose>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                Task delivery = Task.Delay(TimeSpan.FromMinutes(3), clock, CancellationToken.None);
                deliveryStarted.TrySetResult();
                return delivery;
            });
        using BeginDependencies ineligible = CreateBeginDependencies(PasswordRecoveryChannel.Email, clock, useRealDispatcher: true);

        Task<AuthenticationResult<ChallengeResult>> eligibleRequest = eligible.Handler.Handle(
            new(PasswordRecoveryChannel.Email, "person@example.test"), CancellationToken.None);
        Task<AuthenticationResult<ChallengeResult>> ineligibleRequest = ineligible.Handler.Handle(
            new(PasswordRecoveryChannel.Email, "person@example.test"), CancellationToken.None);

        eligibleRequest.IsCompleted.Should().BeFalse();
        ineligibleRequest.IsCompleted.Should().BeFalse();
        await deliveryStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        eligible.EmailSender.Verify(
            x => x.SendAsync("person@example.test", "test-code", AuthenticationChallengePurpose.EmailPasswordRecovery, It.IsAny<CancellationToken>()), Times.Once);

        clock.Advance(AuthenticationChallengePolicy.PasswordRecoveryMinimumResponseDuration);

        (await eligibleRequest).Should().BeOfType<AuthenticationResult<ChallengeResult>.Success>();
        (await ineligibleRequest).Should().BeOfType<AuthenticationResult<ChallengeResult>.Success>();
    }

    [Fact]
    public async Task Begin_slow_delivery_completes_in_the_background_and_leaves_the_challenge_pending()
    {
        FakeTimeProvider clock = new(DateTimeOffset.UtcNow);
        TaskCompletionSource deliveryStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource delivered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using BeginDependencies dependencies = CreateBeginDependencies(PasswordRecoveryChannel.Email, clock, useRealDispatcher: true);
        dependencies.UserDirectory.Setup(x => x.FindByEmailAsync("person@example.test", It.IsAny<CancellationToken>()))
            .ReturnsAsync(User("user-42", email: "person@example.test"));
        dependencies.AccountUpdater.Setup(x => x.UserHasPasswordAsync("user-42", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        dependencies.CodeGenerator.Setup(x => x.GenerateAsync("user-42", It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync("test-code");
        dependencies.EmailSender.Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<AuthenticationChallengePurpose>(), It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                Task delivery = Task.Delay(TimeSpan.FromMinutes(3), clock, CancellationToken.None);
                deliveryStarted.TrySetResult();
                await delivery;
                delivered.SetResult();
            });

        Task<AuthenticationResult<ChallengeResult>> request = dependencies.Handler.Handle(
            new(PasswordRecoveryChannel.Email, "person@example.test"), CancellationToken.None);

        clock.Advance(AuthenticationChallengePolicy.PasswordRecoveryMinimumResponseDuration);
        (await request).Should().BeOfType<AuthenticationResult<ChallengeResult>.Success>();
        await deliveryStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        clock.Advance(TimeSpan.FromMinutes(3));
        await delivered.Task.WaitAsync(TimeSpan.FromSeconds(5));

        dependencies.ChallengeStore.Verify(x => x.ReplaceAsync(It.IsAny<AuthenticationChallenge>(), It.IsAny<CancellationToken>()), Times.Never);
        dependencies.FailureRecorder.Verify(
            x => x.RecordAsync(It.IsAny<PasswordRecoveryChannel>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Begin_delivery_timeout_cancels_the_challenge_and_records_the_failure()
    {
        SignalingFakeTimeProvider clock = new(DateTimeOffset.UtcNow);
        TaskCompletionSource deliveryStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource recorded = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using BeginDependencies dependencies = CreateBeginDependencies(PasswordRecoveryChannel.Email, clock, useRealDispatcher: true);
        dependencies.UserDirectory.Setup(x => x.FindByEmailAsync("person@example.test", It.IsAny<CancellationToken>()))
            .ReturnsAsync(User("user-42", email: "person@example.test"));
        dependencies.AccountUpdater.Setup(x => x.UserHasPasswordAsync("user-42", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        dependencies.CodeGenerator.Setup(x => x.GenerateAsync("user-42", It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync("test-code");
        dependencies.FailureRecorder.Setup(x => x.RecordAsync(It.IsAny<PasswordRecoveryChannel>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback(() => recorded.TrySetResult())
            .Returns(Task.CompletedTask);
        dependencies.EmailSender.Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<AuthenticationChallengePurpose>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                Task delivery = Task.Delay(TimeSpan.FromMinutes(6), clock, CancellationToken.None);
                deliveryStarted.TrySetResult();
                return delivery;
            });

        Task<AuthenticationResult<ChallengeResult>> request = dependencies.Handler.Handle(
            new(PasswordRecoveryChannel.Email, "person@example.test"), CancellationToken.None);

        await deliveryStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await clock.PasswordRecoveryDeliveryTimeoutScheduled.Task.WaitAsync(TimeSpan.FromSeconds(5));

        clock.Advance(AuthenticationChallengePolicy.PasswordRecoveryMinimumResponseDuration);
        (await request).Should().BeOfType<AuthenticationResult<ChallengeResult>.Success>();

        clock.Advance(AuthenticationChallengePolicy.PasswordRecoveryDeliveryTimeout);
        await recorded.Task.WaitAsync(TimeSpan.FromSeconds(5));

        dependencies.FailureRecorder.Verify(
            x => x.RecordAsync(PasswordRecoveryChannel.Email, nameof(TimeoutException), It.IsAny<CancellationToken>()), Times.Once);
        dependencies.ChallengeStore.Verify(x => x.ReplaceAsync(
            It.Is<AuthenticationChallenge>(challenge => challenge.Status == AuthenticationChallengeStatus.Cancelled),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Begin_delivery_exception_cancels_the_challenge_and_records_the_failure()
    {
        FakeTimeProvider clock = new(DateTimeOffset.UtcNow);
        TaskCompletionSource recorded = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using BeginDependencies dependencies = CreateBeginDependencies(PasswordRecoveryChannel.Email, clock, useRealDispatcher: true);
        dependencies.UserDirectory.Setup(x => x.FindByEmailAsync("person@example.test", It.IsAny<CancellationToken>()))
            .ReturnsAsync(User("user-42", email: "person@example.test"));
        dependencies.AccountUpdater.Setup(x => x.UserHasPasswordAsync("user-42", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        dependencies.CodeGenerator.Setup(x => x.GenerateAsync("user-42", It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync("test-code");
        dependencies.FailureRecorder.Setup(x => x.RecordAsync(It.IsAny<PasswordRecoveryChannel>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback(() => recorded.TrySetResult())
            .Returns(Task.CompletedTask);
        dependencies.EmailSender.Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<AuthenticationChallengePurpose>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("contains recipient and provider detail"));

        Task<AuthenticationResult<ChallengeResult>> request = dependencies.Handler.Handle(
            new(PasswordRecoveryChannel.Email, "person@example.test"), CancellationToken.None);

        clock.Advance(AuthenticationChallengePolicy.PasswordRecoveryMinimumResponseDuration);
        (await request).Should().BeOfType<AuthenticationResult<ChallengeResult>.Success>();

        await recorded.Task.WaitAsync(TimeSpan.FromSeconds(5));
        dependencies.FailureRecorder.Verify(
            x => x.RecordAsync(PasswordRecoveryChannel.Email, nameof(IOException), It.IsAny<CancellationToken>()), Times.Once);
        dependencies.ChallengeStore.Verify(x => x.ReplaceAsync(
            It.Is<AuthenticationChallenge>(challenge => challenge.Status == AuthenticationChallengeStatus.Cancelled),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Begin_request_cancellation_after_the_response_does_not_cancel_delivery()
    {
        FakeTimeProvider clock = new(DateTimeOffset.UtcNow);
        using CancellationTokenSource requestCancellation = new();
        TaskCompletionSource<CancellationToken> deliveryStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource delivered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using BeginDependencies dependencies = CreateBeginDependencies(PasswordRecoveryChannel.Email, clock, useRealDispatcher: true);
        dependencies.UserDirectory.Setup(x => x.FindByEmailAsync("person@example.test", It.IsAny<CancellationToken>()))
            .ReturnsAsync(User("user-42", email: "person@example.test"));
        dependencies.AccountUpdater.Setup(x => x.UserHasPasswordAsync("user-42", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        dependencies.CodeGenerator.Setup(x => x.GenerateAsync("user-42", It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync("test-code");
        dependencies.EmailSender.Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<AuthenticationChallengePurpose>(), It.IsAny<CancellationToken>()))
            .Returns<string, string, AuthenticationChallengePurpose, CancellationToken>((_, _, _, deliveryToken) =>
            {
                return DeliverAsync();
                async Task DeliverAsync()
                {
                    // Signal only after the fake timer exists, so the test can
                    // advance the clock without racing the registration.
                    Task delivery = Task.Delay(TimeSpan.FromMinutes(3), clock, deliveryToken);
                    deliveryStarted.TrySetResult(deliveryToken);
                    await delivery;
                    delivered.SetResult();
                }
            });

        Task<AuthenticationResult<ChallengeResult>> request = dependencies.Handler.Handle(
            new(PasswordRecoveryChannel.Email, "person@example.test"), requestCancellation.Token);

        clock.Advance(AuthenticationChallengePolicy.PasswordRecoveryMinimumResponseDuration);
        (await request).Should().BeOfType<AuthenticationResult<ChallengeResult>.Success>();

        requestCancellation.Cancel();
        CancellationToken deliveryToken = await deliveryStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        deliveryToken.IsCancellationRequested.Should().BeFalse();

        clock.Advance(TimeSpan.FromMinutes(3));
        await delivered.Task.WaitAsync(TimeSpan.FromSeconds(5));

        dependencies.ChallengeStore.Verify(x => x.ReplaceAsync(It.IsAny<AuthenticationChallenge>(), It.IsAny<CancellationToken>()), Times.Never);
        dependencies.FailureRecorder.Verify(
            x => x.RecordAsync(It.IsAny<PasswordRecoveryChannel>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
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
        dependencies.Dispatcher!.Verify(x => x.DispatchAsync(
            It.Is<AuthenticationChallenge>(challenge =>
                challenge.Destination == "Person@Example.test" &&
                challenge.Purpose == AuthenticationChallengePurpose.EmailPasswordRecovery),
            "test-code",
            It.IsAny<CancellationToken>()), Times.Once);
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
        dependencies.Dispatcher!.Verify(x => x.DispatchAsync(
            It.Is<AuthenticationChallenge>(challenge =>
                challenge.Destination == "+989123456789" &&
                challenge.Purpose == AuthenticationChallengePurpose.PhonePasswordRecovery),
            "test-code",
            It.IsAny<CancellationToken>()), Times.Once);
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
        unconvertible.Dispatcher!.Verify(x => x.DispatchAsync(It.IsAny<AuthenticationChallenge>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        unknown.Dispatcher!.Verify(x => x.DispatchAsync(It.IsAny<AuthenticationChallenge>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
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
        dependencies.Dispatcher!.Verify(x => x.DispatchAsync(
            It.Is<AuthenticationChallenge>(challenge =>
                challenge.Destination == "+15550101" &&
                challenge.Purpose == AuthenticationChallengePurpose.PhonePasswordRecovery),
            "test-code",
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
            new InMemoryAuthenticationAttemptTracker(), CreateKeys(), policy.Object, TestSourceAttemptGuards.WithoutClientAddress(), TimeProvider.System);

        AuthenticationResult<bool> result = await handler.Handle(
            new("challenge-id", "code", "StrongPass1!"), CancellationToken.None);

        result.Should().BeOfType<AuthenticationResult<bool>.Failure>().Which.Error.Reason
            .Should().Be(AuthenticationFailureReason.NotAllowed);
        store.Verify(x => x.GetAsync(It.IsAny<AuthenticationChallengeId>(), It.IsAny<CancellationToken>()), Times.Never);
        verifier.Verify(x => x.VerifyAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        updater.Verify(x => x.ResetPasswordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static BeginDependencies CreateBeginDependencies(PasswordRecoveryChannel channel, FakeTimeProvider? clock = null, bool useRealDispatcher = false)
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

        Mock<IPasswordRecoveryCodeDispatcher>? dispatcherMock = null;
        ServiceProvider? dispatcherProvider = null;
        IPasswordRecoveryCodeDispatcher dispatcher;
        if (useRealDispatcher)
        {
            FakeTimeProvider timeProvider = clock ?? throw new ArgumentNullException(nameof(clock));
            ServiceCollection services = new();
            services.AddSingleton(store.Object);
            services.AddSingleton(recorder.Object);
            services.AddScoped<IEmailAuthenticationCodeSender>(_ => emailSender.Object);
            services.AddScoped<IPhoneAuthenticationCodeSender>(_ => phoneSender.Object);
            dispatcherProvider = services.BuildServiceProvider();
            dispatcher = new PasswordRecoveryCodeDispatcher(
                dispatcherProvider.GetRequiredService<IServiceScopeFactory>(),
                timeProvider,
                NullLogger<PasswordRecoveryCodeDispatcher>.Instance);
        }
        else
        {
            dispatcherMock = new();
            dispatcherMock.Setup(x => x.DispatchAsync(It.IsAny<AuthenticationChallenge>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            dispatcher = dispatcherMock.Object;
        }

        var handler = new BeginPasswordRecoveryHandler(store.Object, userDirectory.Object, generator.Object,
            dispatcher, updater.Object, attempts.Object, CreateKeys(), recorder.Object,
            AuthenticationChallengePolicy.Default, policy.Object, TestSourceAttemptGuards.WithoutClientAddress(), clock ?? TimeProvider.System);
        return new(handler, store, userDirectory, generator, emailSender, phoneSender, updater, attempts, recorder, policy, dispatcherMock, dispatcherProvider);
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
            CreateKeys(), policy.Object, TestSourceAttemptGuards.WithoutClientAddress(), TimeProvider.System);
        return new(handler, store, verifier, updater, policy, challenge);
    }

    private static AuthenticationChallenge Challenge(DateTimeOffset now) => AuthenticationChallengePolicy.Default
        .CreateEmailPasswordRecoveryChallenge("user-42", "person@example.test", now);

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
        Mock<IAuthenticationSecurityPolicyProvider> SecurityPolicy,
        Mock<IPasswordRecoveryCodeDispatcher>? Dispatcher,
        ServiceProvider? DispatcherProvider) : IDisposable
    {
        public void Dispose() => DispatcherProvider?.Dispose();
    }

    private sealed record CompleteDependencies(
        CompletePasswordRecoveryHandler Handler,
        InMemoryAuthenticationChallengeStore ChallengeStore,
        Mock<IAuthenticationCodeVerifier> CodeVerifier,
        Mock<IUserAccountUpdater> AccountUpdater,
        Mock<IAuthenticationSecurityPolicyProvider> SecurityPolicy,
        AuthenticationChallenge Challenge);

    private sealed class SignalingFakeTimeProvider(DateTimeOffset start) : FakeTimeProvider(start)
    {
        public TaskCompletionSource PasswordRecoveryDeliveryTimeoutScheduled { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            ITimer timer = base.CreateTimer(callback, state, dueTime, period);
            if (dueTime == AuthenticationChallengePolicy.PasswordRecoveryDeliveryTimeout)
                PasswordRecoveryDeliveryTimeoutScheduled.TrySetResult();
            return timer;
        }
    }

    private enum BeginScenario
    {
        Unknown,
        Passwordless,
        Denied,
        Eligible
    }
}
