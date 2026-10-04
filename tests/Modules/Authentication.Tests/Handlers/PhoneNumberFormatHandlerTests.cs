using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Application.Commands;
using Vitarish.Authentication.Application.Handlers;
using Vitarish.Authentication.Application.Results;
using Vitarish.Authentication.Domain.Challenges;
using Vitarish.Authentication.Domain.Security;
using Vitarish.Authentication.Infrastructure;
using Vitarish.Authentication.Infrastructure.Options;
using Vitarish.Users.Domain.Security;

namespace Vitarish.Authentication.Tests.Handlers;

public sealed class PhoneNumberFormatHandlerTests
{
    [Fact]
    public async Task Phone_login_converts_a_local_format_number_before_lookup_and_sms()
    {
        const string localPhoneNumber = "09123456789";
        const string e164PhoneNumber = "+989123456789";
        Mock<IAuthenticationChallengeStore> challengeStore = new();
        Mock<IAuthenticationUserDirectory> userDirectory = new(MockBehavior.Strict);
        userDirectory.Setup(value => value.FindOrCreateByPhoneNumberAsync(e164PhoneNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthenticationUserDescriptor("user-1", e164PhoneNumber, null, null, null, null, e164PhoneNumber, false));
        Mock<IAuthenticationCodeGenerator> codeGenerator = new();
        codeGenerator.Setup(value => value.GenerateAsync("user-1", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("654321");
        Mock<IPhoneAuthenticationCodeSender> phoneSender = new();
        Mock<IAuthenticationAttemptTracker> attemptTracker = new();
        attemptTracker.Setup(value => value.IsDeniedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        Mock<IAuthenticationSecurityPolicyProvider> policyProvider = new();
        policyProvider.Setup(value => value.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(SecurityAccessPolicy.Default);
        BeginPhoneLoginHandler handler = new(
            challengeStore.Object,
            userDirectory.Object,
            codeGenerator.Object,
            phoneSender.Object,
            AuthenticationChallengePolicy.Default,
            attemptTracker.Object,
            policyProvider.Object,
            new AuthenticationAttemptKeyFactory(Options.Create(new AuthenticationAttemptKeyOptions
            {
                Realm = "tests",
                CurrentPepper = "test-authentication-attempt-key-pepper-20260829"
            })));

        AuthenticationResult<ChallengeResult> result = await handler.Handle(
            new BeginPhoneLoginCommand(localPhoneNumber, null),
            CancellationToken.None);

        result.Should().BeOfType<AuthenticationResult<ChallengeResult>.Success>();
        userDirectory.Verify(value => value.FindOrCreateByPhoneNumberAsync(e164PhoneNumber, It.IsAny<CancellationToken>()), Times.Once);
        userDirectory.VerifyNoOtherCalls();
        phoneSender.Verify(value => value.SendAsync(e164PhoneNumber, "654321", AuthenticationChallengePurpose.PhoneLogin, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("2025550123")]
    [InlineData("+0123456789")]
    [InlineData("+1234567890123456")]
    [InlineData("+982112345678")]
    [InlineData("abc")]
    public async Task Phone_login_rejects_an_unconvertible_number_without_any_directory_store_or_sms_call(string phoneNumber)
    {
        Mock<IAuthenticationUserDirectory> userDirectory = new(MockBehavior.Strict);
        Mock<IAuthenticationChallengeStore> challengeStore = new(MockBehavior.Strict);
        Mock<IAuthenticationCodeGenerator> codeGenerator = new(MockBehavior.Strict);
        Mock<IPhoneAuthenticationCodeSender> phoneSender = new(MockBehavior.Strict);
        Mock<IAuthenticationAttemptTracker> attemptTracker = new();
        attemptTracker.Setup(value => value.IsDeniedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        Mock<IAuthenticationSecurityPolicyProvider> policyProvider = new();
        policyProvider.Setup(value => value.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(SecurityAccessPolicy.Default);
        BeginPhoneLoginHandler handler = new(
            challengeStore.Object,
            userDirectory.Object,
            codeGenerator.Object,
            phoneSender.Object,
            AuthenticationChallengePolicy.Default,
            attemptTracker.Object,
            policyProvider.Object,
            new AuthenticationAttemptKeyFactory(Options.Create(new AuthenticationAttemptKeyOptions
            {
                Realm = "tests",
                CurrentPepper = "test-authentication-attempt-key-pepper-20260829"
            })));

        AuthenticationResult<ChallengeResult> result = await handler.Handle(
            new BeginPhoneLoginCommand(phoneNumber, null),
            CancellationToken.None);

        result.Should().BeOfType<AuthenticationResult<ChallengeResult>.Failure>()
            .Which.Error.Should().Match<AuthenticationFailure>(failure =>
                failure.Reason == AuthenticationFailureReason.InvalidInput &&
                failure.Message == UserPhoneNumberPolicy.InvalidPhoneNumberMessage);
        userDirectory.VerifyNoOtherCalls();
        challengeStore.VerifyNoOtherCalls();
        codeGenerator.VerifyNoOtherCalls();
        phoneSender.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("2025550123")]
    [InlineData("+0123456789")]
    [InlineData("+1234567890123456")]
    [InlineData("+982112345678")]
    [InlineData("abc")]
    public async Task Phone_change_rejects_non_E164_numbers_before_creating_a_challenge(string phoneNumber)
    {
        Mock<IAuthenticationUserDirectory> userDirectory = new(MockBehavior.Strict);
        Mock<IAuthenticationChallengeStore> challengeStore = new(MockBehavior.Strict);
        Mock<IAuthenticationCodeGenerator> codeGenerator = new(MockBehavior.Strict);
        Mock<IPhoneAuthenticationCodeSender> phoneSender = new(MockBehavior.Strict);
        BeginChangePhoneHandler handler = new(
            challengeStore.Object,
            userDirectory.Object,
            codeGenerator.Object,
            phoneSender.Object,
            AuthenticationChallengePolicy.Default);

        AuthenticationResult<ChallengeResult> result = await handler.Handle(
            new BeginChangePhoneCommand("user-id", phoneNumber),
            CancellationToken.None);

        result.Should().BeOfType<AuthenticationResult<ChallengeResult>.Failure>()
            .Which.Error.Reason.Should().Be(AuthenticationFailureReason.InvalidInput);
        userDirectory.VerifyNoOtherCalls();
        challengeStore.VerifyNoOtherCalls();
        codeGenerator.VerifyNoOtherCalls();
        phoneSender.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Phone_change_converts_a_local_format_number_before_the_challenge_and_sms()
    {
        Mock<IAuthenticationChallengeStore> challengeStore = new();
        Mock<IAuthenticationUserDirectory> userDirectory = new(MockBehavior.Strict);
        Mock<IAuthenticationCodeGenerator> codeGenerator = new();
        codeGenerator.Setup(value => value.GenerateAsync("user-id", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("654321");
        Mock<IPhoneAuthenticationCodeSender> phoneSender = new();
        BeginChangePhoneHandler handler = new(
            challengeStore.Object,
            userDirectory.Object,
            codeGenerator.Object,
            phoneSender.Object,
            AuthenticationChallengePolicy.Default);

        AuthenticationResult<ChallengeResult> result = await handler.Handle(
            new BeginChangePhoneCommand("user-id", "۰۹۱۲۳۴۵۶۷۸۹"), CancellationToken.None);

        result.Should().BeOfType<AuthenticationResult<ChallengeResult>.Success>();
        challengeStore.Verify(value => value.SaveAsync(
            It.Is<AuthenticationChallenge>(challenge => challenge.Destination == "+989123456789"),
            It.IsAny<CancellationToken>()), Times.Once);
        phoneSender.Verify(value => value.SendAsync(
            "+989123456789",
            "654321",
            AuthenticationChallengePurpose.PhoneChange,
            It.IsAny<CancellationToken>()), Times.Once);
        userDirectory.VerifyNoOtherCalls();
    }
}
