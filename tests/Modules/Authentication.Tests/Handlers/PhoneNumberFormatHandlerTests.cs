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

namespace Vitarish.Authentication.Tests.Handlers;

public sealed class PhoneNumberFormatHandlerTests
{
    [Fact]
    public async Task Phone_login_preserves_the_legacy_path_for_a_stored_non_E164_number()
    {
        const string legacyPhoneNumber = "09121234567";
        Mock<IAuthenticationChallengeStore> challengeStore = new();
        Mock<IAuthenticationUserDirectory> userDirectory = new(MockBehavior.Strict);
        userDirectory.Setup(value => value.FindByPhoneNumberAsync(legacyPhoneNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthenticationUserDescriptor("legacy-user", legacyPhoneNumber, null, null, null, null, legacyPhoneNumber, false));
        Mock<IAuthenticationCodeGenerator> codeGenerator = new();
        codeGenerator.Setup(value => value.GenerateAsync("legacy-user", It.IsAny<string>(), It.IsAny<CancellationToken>()))
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
            new BeginPhoneLoginCommand(legacyPhoneNumber, null),
            CancellationToken.None);

        result.Should().BeOfType<AuthenticationResult<ChallengeResult>.Success>();
        userDirectory.Verify(value => value.FindByPhoneNumberAsync(legacyPhoneNumber, It.IsAny<CancellationToken>()), Times.Once);
        userDirectory.VerifyNoOtherCalls();
        phoneSender.Verify(value => value.SendAsync(legacyPhoneNumber, "654321", AuthenticationChallengePurpose.PhoneLogin, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("2025550123")]
    [InlineData("+0123456789")]
    [InlineData("+1234567890123456")]
    public async Task Phone_login_rejects_an_unknown_non_E164_number_without_creating_an_account_or_challenge(string phoneNumber)
    {
        Mock<IAuthenticationUserDirectory> userDirectory = new(MockBehavior.Strict);
        userDirectory.Setup(value => value.FindByPhoneNumberAsync(phoneNumber, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AuthenticationUserDescriptor?)null);
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
            .Which.Error.Reason.Should().Be(AuthenticationFailureReason.InvalidInput);
        userDirectory.Verify(value => value.FindByPhoneNumberAsync(phoneNumber, It.IsAny<CancellationToken>()), Times.Once);
        userDirectory.VerifyNoOtherCalls();
        challengeStore.VerifyNoOtherCalls();
        codeGenerator.VerifyNoOtherCalls();
        phoneSender.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("2025550123")]
    [InlineData("+0123456789")]
    [InlineData("+1234567890123456")]
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
}
