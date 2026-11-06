using FluentAssertions;
using Moq;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Application.Commands;
using Vitarish.Authentication.Application.Handlers;
using Vitarish.Authentication.Application.Results;
using Vitarish.Authentication.Domain.Challenges;
using Vitarish.Authentication.Domain.Security;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Authentication.Tests.Handlers;

public sealed class CompleteContactChangeSubjectBindingTests
{
    private const string CurrentUserId = "current-user";
    private const string OtherUserId = "other-user";
    private const string VerificationCode = "123456";

    [Fact]
    public async Task Email_completion_rejects_a_request_context_subject_mismatch_before_challenge_lookup()
    {
        Mock<IAuthenticationChallengeStore> store = new(MockBehavior.Strict);
        Mock<IAuthenticationCodeVerifier> verifier = new(MockBehavior.Strict);
        Mock<IUserAccountUpdater> updater = new(MockBehavior.Strict);
        CompleteChangeEmailHandler handler = new(store.Object, verifier.Object, updater.Object, RequestContext(OtherUserId).Object);

        AuthenticationResult<bool> result = await handler.Handle(
            new CompleteChangeEmailCommand(CurrentUserId, "challenge-id", VerificationCode),
            CancellationToken.None);

        result.Should().BeOfType<AuthenticationResult<bool>.Failure>()
            .Which.Error.Reason.Should().Be(AuthenticationFailureReason.Unauthorized);
        store.VerifyNoOtherCalls();
        verifier.VerifyNoOtherCalls();
        updater.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Phone_completion_rejects_a_request_context_subject_mismatch_before_challenge_lookup()
    {
        Mock<IAuthenticationChallengeStore> store = new(MockBehavior.Strict);
        Mock<IAuthenticationCodeVerifier> verifier = new(MockBehavior.Strict);
        Mock<IUserAccountUpdater> updater = new(MockBehavior.Strict);
        CompleteChangePhoneHandler handler = new(store.Object, verifier.Object, updater.Object, RequestContext(OtherUserId).Object);

        AuthenticationResult<bool> result = await handler.Handle(
            new CompleteChangePhoneCommand(CurrentUserId, "challenge-id", VerificationCode),
            CancellationToken.None);

        result.Should().BeOfType<AuthenticationResult<bool>.Failure>()
            .Which.Error.Reason.Should().Be(AuthenticationFailureReason.Unauthorized);
        store.VerifyNoOtherCalls();
        verifier.VerifyNoOtherCalls();
        updater.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Email_completion_rejects_a_challenge_owned_by_another_user_before_verifying_or_mutating()
    {
        AuthenticationChallenge challenge = AuthenticationChallengePolicy.Default.CreateEmailChangeChallenge(
            OtherUserId,
            "changed@example.test",
            DateTimeOffset.UtcNow);
        Mock<IAuthenticationChallengeStore> store = ChallengeStore(challenge);
        Mock<IAuthenticationCodeVerifier> verifier = new(MockBehavior.Strict);
        Mock<IUserAccountUpdater> updater = new(MockBehavior.Strict);
        CompleteChangeEmailHandler handler = new(store.Object, verifier.Object, updater.Object, RequestContext(CurrentUserId).Object);

        AuthenticationResult<bool> result = await handler.Handle(
            new CompleteChangeEmailCommand(CurrentUserId, challenge.Id.Value, VerificationCode),
            CancellationToken.None);

        result.Should().BeOfType<AuthenticationResult<bool>.Failure>()
            .Which.Error.Reason.Should().Be(AuthenticationFailureReason.InvalidCredentials);
        store.Verify(item => item.GetAsync(challenge.Id, It.IsAny<CancellationToken>()), Times.Once);
        store.Verify(item => item.TryReserveAttemptAsync(It.IsAny<AuthenticationChallenge>(), It.IsAny<CancellationToken>()), Times.Never);
        store.Verify(item => item.TryConsumeAsync(It.IsAny<AuthenticationChallenge>(), It.IsAny<CancellationToken>()), Times.Never);
        store.VerifyNoOtherCalls();
        verifier.VerifyNoOtherCalls();
        updater.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Phone_completion_rejects_a_challenge_owned_by_another_user_before_verifying_or_mutating()
    {
        AuthenticationChallenge challenge = AuthenticationChallengePolicy.Default.CreatePhoneChangeChallenge(
            OtherUserId,
            "+12025550123",
            DateTimeOffset.UtcNow);
        Mock<IAuthenticationChallengeStore> store = ChallengeStore(challenge);
        Mock<IAuthenticationCodeVerifier> verifier = new(MockBehavior.Strict);
        Mock<IUserAccountUpdater> updater = new(MockBehavior.Strict);
        CompleteChangePhoneHandler handler = new(store.Object, verifier.Object, updater.Object, RequestContext(CurrentUserId).Object);

        AuthenticationResult<bool> result = await handler.Handle(
            new CompleteChangePhoneCommand(CurrentUserId, challenge.Id.Value, VerificationCode),
            CancellationToken.None);

        result.Should().BeOfType<AuthenticationResult<bool>.Failure>()
            .Which.Error.Reason.Should().Be(AuthenticationFailureReason.InvalidCredentials);
        store.Verify(item => item.GetAsync(challenge.Id, It.IsAny<CancellationToken>()), Times.Once);
        store.Verify(item => item.TryReserveAttemptAsync(It.IsAny<AuthenticationChallenge>(), It.IsAny<CancellationToken>()), Times.Never);
        store.Verify(item => item.TryConsumeAsync(It.IsAny<AuthenticationChallenge>(), It.IsAny<CancellationToken>()), Times.Never);
        store.VerifyNoOtherCalls();
        verifier.VerifyNoOtherCalls();
        updater.VerifyNoOtherCalls();
    }

    private static Mock<IAuthenticationChallengeStore> ChallengeStore(AuthenticationChallenge challenge)
    {
        Mock<IAuthenticationChallengeStore> store = new(MockBehavior.Strict);
        store.Setup(item => item.GetAsync(challenge.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(challenge);
        return store;
    }

    private static Mock<IRequestContext> RequestContext(string? userId)
    {
        Mock<IRequestContext> context = new(MockBehavior.Strict);
        context.SetupGet(item => item.UserId).Returns(userId);
        return context;
    }
}
