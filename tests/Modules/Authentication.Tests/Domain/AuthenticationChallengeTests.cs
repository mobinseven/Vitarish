using FluentAssertions;
using Vitarish.Authentication.Domain.Challenges;
using Vitarish.Authentication.Domain.Security;

namespace Authentication.Tests.Domain;

public class AuthenticationChallengeTests
{
    private static AuthenticationChallenge CreateChallenge(
        int maximumAttempts = 5,
        DateTimeOffset? expiresAt = null)
    {
        var now = DateTimeOffset.UtcNow;
        return new AuthenticationChallenge(
            new AuthenticationChallengeId("test-id"),
            AuthenticationChallengePurpose.EmailLogin,
            "user-1",
            "test@example.com",
            "Authentication:EmailLogin",
            now,
            expiresAt ?? now.AddMinutes(5),
            maximumAttempts);
    }

    [Fact]
    public void Constructor_enforces_minimum_attempts()
    {
        var act = () => new AuthenticationChallenge(
            new AuthenticationChallengeId("id"),
            AuthenticationChallengePurpose.EmailLogin,
            "user",
            "dest",
            "token",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddMinutes(5),
            maximumAttempts: 0);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("maximumAttempts");
    }

    [Fact]
    public void IsExpired_returns_true_when_past_expiry()
    {
        var challenge = CreateChallenge(expiresAt: DateTimeOffset.UtcNow.AddMinutes(-1));

        challenge.IsExpired(DateTimeOffset.UtcNow).Should().BeTrue();
    }

    [Fact]
    public void IsExpired_returns_false_when_within_lifetime()
    {
        var challenge = CreateChallenge();

        challenge.IsExpired(DateTimeOffset.UtcNow).Should().BeFalse();
    }

    [Fact]
    public void CanAttempt_returns_true_for_pending_unexpired_challenge_with_attempts_remaining()
    {
        var challenge = CreateChallenge();

        challenge.CanAttempt(DateTimeOffset.UtcNow).Should().BeTrue();
    }

    [Fact]
    public void CanAttempt_returns_false_when_exhausted()
    {
        var challenge = CreateChallenge(maximumAttempts: 1);
        challenge.RegisterFailure(DateTimeOffset.UtcNow);

        challenge.CanAttempt(DateTimeOffset.UtcNow).Should().BeFalse();
    }

    [Fact]
    public void CanAttempt_returns_false_when_completed()
    {
        var challenge = CreateChallenge();
        challenge.Complete(DateTimeOffset.UtcNow);

        challenge.CanAttempt(DateTimeOffset.UtcNow).Should().BeFalse();
    }

    [Fact]
    public void CanAttempt_returns_false_when_expired()
    {
        var challenge = CreateChallenge(expiresAt: DateTimeOffset.UtcNow.AddMinutes(-1));

        challenge.CanAttempt(DateTimeOffset.UtcNow).Should().BeFalse();
    }

    [Fact]
    public void CanAttempt_returns_false_when_cancelled()
    {
        var challenge = CreateChallenge();
        challenge.Cancel();

        challenge.CanAttempt(DateTimeOffset.UtcNow).Should().BeFalse();
    }

    [Fact]
    public void Complete_transitions_to_completed_status()
    {
        var challenge = CreateChallenge();
        var now = DateTimeOffset.UtcNow;

        challenge.Complete(now);

        challenge.Status.Should().Be(AuthenticationChallengeStatus.Completed);
        challenge.CompletedAt.Should().Be(now);
    }

    [Fact]
    public void Complete_throws_when_already_completed()
    {
        var challenge = CreateChallenge();
        challenge.Complete(DateTimeOffset.UtcNow);

        var act = () => challenge.Complete(DateTimeOffset.UtcNow);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*already been completed*");
    }

    [Fact]
    public void Complete_throws_when_expired()
    {
        var challenge = CreateChallenge(expiresAt: DateTimeOffset.UtcNow.AddMinutes(-1));

        var act = () => challenge.Complete(DateTimeOffset.UtcNow);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*expired*");
    }

    [Fact]
    public void Complete_throws_when_exhausted()
    {
        var challenge = CreateChallenge(maximumAttempts: 1);
        challenge.RegisterFailure(DateTimeOffset.UtcNow);

        var act = () => challenge.Complete(DateTimeOffset.UtcNow);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*exhausted*");
    }

    [Fact]
    public void RegisterFailure_increments_failed_attempts()
    {
        var challenge = CreateChallenge();

        challenge.RegisterFailure(DateTimeOffset.UtcNow);

        challenge.FailedAttempts.Should().Be(1);
    }

    [Fact]
    public void RegisterFailure_sets_exhausted_when_no_attempts_remaining()
    {
        var challenge = CreateChallenge(maximumAttempts: 1);

        challenge.RegisterFailure(DateTimeOffset.UtcNow);

        challenge.Status.Should().Be(AuthenticationChallengeStatus.Exhausted);
    }

    [Fact]
    public void RegisterFailure_sets_expired_when_past_expiry()
    {
        var challenge = CreateChallenge(expiresAt: DateTimeOffset.UtcNow.AddMinutes(-1));

        challenge.RegisterFailure(DateTimeOffset.UtcNow);

        challenge.Status.Should().Be(AuthenticationChallengeStatus.Expired);
    }

    [Fact]
    public void RegisterFailure_does_not_increment_when_completed()
    {
        var challenge = CreateChallenge();
        challenge.Complete(DateTimeOffset.UtcNow);

        challenge.RegisterFailure(DateTimeOffset.UtcNow);

        challenge.FailedAttempts.Should().Be(0);
    }

    [Fact]
    public void Cancel_transitions_to_cancelled_status()
    {
        var challenge = CreateChallenge();

        challenge.Cancel();

        challenge.Status.Should().Be(AuthenticationChallengeStatus.Cancelled);
    }

    [Fact]
    public void Cancel_does_not_affect_completed_challenge()
    {
        var challenge = CreateChallenge();
        challenge.Complete(DateTimeOffset.UtcNow);

        challenge.Cancel();

        challenge.Status.Should().Be(AuthenticationChallengeStatus.Completed);
    }

    [Fact]
    public void RemainingAttempts_decrements_with_failures()
    {
        var challenge = CreateChallenge(maximumAttempts: 5);

        challenge.RegisterFailure(DateTimeOffset.UtcNow);
        challenge.RegisterFailure(DateTimeOffset.UtcNow);

        challenge.RemainingAttempts.Should().Be(3);
    }

    [Fact]
    public void RemainingAttempts_does_not_go_below_zero()
    {
        var challenge = CreateChallenge(maximumAttempts: 1);
        challenge.RegisterFailure(DateTimeOffset.UtcNow);
        challenge.RegisterFailure(DateTimeOffset.UtcNow);

        challenge.RemainingAttempts.Should().Be(0);
    }

    [Fact]
    public void ValidateAttempt_returns_null_for_valid_pending_challenge()
    {
        var challenge = CreateChallenge();

        var result = challenge.ValidateAttempt(DateTimeOffset.UtcNow, AuthenticationChallengePurpose.EmailLogin);

        result.Should().BeNull();
    }

    [Fact]
    public void ValidateAttempt_returns_invalid_input_for_wrong_purpose()
    {
        var challenge = CreateChallenge();

        var result = challenge.ValidateAttempt(DateTimeOffset.UtcNow, AuthenticationChallengePurpose.PhoneLogin);

        result.Should().Be(AuthenticationFailureReason.InvalidInput);
    }

    [Fact]
    public void ValidateAttempt_returns_challenge_already_used_when_completed()
    {
        var challenge = CreateChallenge();
        challenge.Complete(DateTimeOffset.UtcNow);

        var result = challenge.ValidateAttempt(DateTimeOffset.UtcNow, AuthenticationChallengePurpose.EmailLogin);

        result.Should().Be(AuthenticationFailureReason.ChallengeAlreadyUsed);
    }

    [Fact]
    public void ValidateAttempt_returns_challenge_expired_when_past_expiry()
    {
        var challenge = CreateChallenge(expiresAt: DateTimeOffset.UtcNow.AddMinutes(-1));

        var result = challenge.ValidateAttempt(DateTimeOffset.UtcNow, AuthenticationChallengePurpose.EmailLogin);

        result.Should().Be(AuthenticationFailureReason.ChallengeExpired);
    }

    [Fact]
    public void ValidateAttempt_returns_challenge_attempts_exhausted()
    {
        var challenge = CreateChallenge(maximumAttempts: 1);
        challenge.RegisterFailure(DateTimeOffset.UtcNow);

        var result = challenge.ValidateAttempt(DateTimeOffset.UtcNow, AuthenticationChallengePurpose.EmailLogin);

        result.Should().Be(AuthenticationFailureReason.ChallengeAttemptsExhausted);
    }

    [Fact]
    public void Restore_reconstitutes_challenge_from_persisted_state()
    {
        var now = DateTimeOffset.UtcNow;
        var restored = AuthenticationChallenge.Restore(
            new AuthenticationChallengeId("restored-id"),
            AuthenticationChallengePurpose.PhoneLogin,
            "user-2",
            "+1234567890",
            "Authentication:PhoneLogin",
            now.AddMinutes(-2),
            now.AddMinutes(3),
            maximumAttempts: 5,
            failedAttempts: 2,
            AuthenticationChallengeStatus.Pending,
            null);

        restored.Id.Value.Should().Be("restored-id");
        restored.Purpose.Should().Be(AuthenticationChallengePurpose.PhoneLogin);
        restored.FailedAttempts.Should().Be(2);
        restored.RemainingAttempts.Should().Be(3);
        restored.Status.Should().Be(AuthenticationChallengeStatus.Pending);
    }
}
