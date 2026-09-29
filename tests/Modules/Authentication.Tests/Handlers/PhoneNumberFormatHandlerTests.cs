using FluentAssertions;
using Moq;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Application.Commands;
using Vitarish.Authentication.Application.Handlers;
using Vitarish.Authentication.Application.Results;
using Vitarish.Authentication.Domain.Challenges;
using Vitarish.Authentication.Domain.Security;

namespace Vitarish.Authentication.Tests.Handlers;

public sealed class PhoneNumberFormatHandlerTests
{
    [Theory]
    [InlineData("2025550123")]
    [InlineData("+0123456789")]
    [InlineData("+1234567890123456")]
    public async Task Phone_login_rejects_non_E164_numbers_before_creating_an_account_or_challenge(string phoneNumber)
    {
        Mock<IAuthenticationUserDirectory> userDirectory = new(MockBehavior.Strict);
        Mock<IAuthenticationChallengeStore> challengeStore = new(MockBehavior.Strict);
        Mock<IAuthenticationCodeGenerator> codeGenerator = new(MockBehavior.Strict);
        Mock<IPhoneAuthenticationCodeSender> phoneSender = new(MockBehavior.Strict);
        Mock<IAuthenticationAttemptTracker> attemptTracker = new(MockBehavior.Strict);
        Mock<IAuthenticationSecurityPolicyProvider> policyProvider = new(MockBehavior.Strict);
        Mock<IAuthenticationAttemptKeyFactory> attemptKeyFactory = new(MockBehavior.Strict);
        BeginPhoneLoginHandler handler = new(
            challengeStore.Object,
            userDirectory.Object,
            codeGenerator.Object,
            phoneSender.Object,
            AuthenticationChallengePolicy.Default,
            attemptTracker.Object,
            policyProvider.Object,
            attemptKeyFactory.Object);

        AuthenticationResult<ChallengeResult> result = await handler.Handle(
            new BeginPhoneLoginCommand(phoneNumber, null),
            CancellationToken.None);

        result.Should().BeOfType<AuthenticationResult<ChallengeResult>.Failure>()
            .Which.Error.Reason.Should().Be(AuthenticationFailureReason.InvalidInput);
        userDirectory.VerifyNoOtherCalls();
        challengeStore.VerifyNoOtherCalls();
        codeGenerator.VerifyNoOtherCalls();
        phoneSender.VerifyNoOtherCalls();
        attemptTracker.VerifyNoOtherCalls();
        policyProvider.VerifyNoOtherCalls();
        attemptKeyFactory.VerifyNoOtherCalls();
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
