using FluentAssertions;
using Vitarish.Authentication.Domain.Challenges;

namespace Authentication.Tests.Domain;

public class AuthenticationChallengePolicyTests
{
    [Fact]
    public void Default_has_five_minute_lifetime()
    {
        AuthenticationChallengePolicy.Default.Lifetime.Should().Be(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void Default_lifetime_covers_the_password_recovery_delivery_timeout()
    {
        AuthenticationChallengePolicy.PasswordRecoveryDeliveryTimeout.Should().Be(TimeSpan.FromMinutes(5));
        AuthenticationChallengePolicy.Default.Lifetime.Should().BeGreaterThanOrEqualTo(AuthenticationChallengePolicy.PasswordRecoveryDeliveryTimeout);
    }

    [Fact]
    public void Default_has_five_maximum_attempts()
    {
        AuthenticationChallengePolicy.Default.MaximumAttempts.Should().Be(5);
    }

    [Fact]
    public void IsExpired_returns_true_after_lifetime()
    {
        var createdAt = DateTimeOffset.UtcNow.AddMinutes(-6);

        AuthenticationChallengePolicy.Default.IsExpired(createdAt, DateTimeOffset.UtcNow).Should().BeTrue();
    }

    [Fact]
    public void IsExpired_returns_false_within_lifetime()
    {
        var createdAt = DateTimeOffset.UtcNow;

        AuthenticationChallengePolicy.Default.IsExpired(createdAt, DateTimeOffset.UtcNow).Should().BeFalse();
    }

    [Fact]
    public void HasAttemptsRemaining_returns_true_below_maximum()
    {
        AuthenticationChallengePolicy.Default.HasAttemptsRemaining(4).Should().BeTrue();
    }

    [Fact]
    public void HasAttemptsRemaining_returns_false_at_maximum()
    {
        AuthenticationChallengePolicy.Default.HasAttemptsRemaining(5).Should().BeFalse();
    }

    [Fact]
    public void CreatePhoneLoginChallenge_binds_token_purpose_to_the_challenge()
    {
        var now = DateTimeOffset.UtcNow;

        var challenge = AuthenticationChallengePolicy.Default
            .CreatePhoneLoginChallenge("user-1", "+1234567890", now);

        challenge.Purpose.Should().Be(AuthenticationChallengePurpose.PhoneLogin);
        challenge.UserId.Should().Be("user-1");
        challenge.Destination.Should().Be("+1234567890");
        challenge.CreatedAt.Should().Be(now);
        challenge.ExpiresAt.Should().Be(now.AddMinutes(5));
        challenge.MaximumAttempts.Should().Be(5);
        challenge.TokenPurpose.Should().Be($"Authentication:PhoneLogin:{challenge.Id.Value}");
    }

    [Fact]
    public void CreateEmailLoginChallenge_creates_challenge_with_correct_purpose()
    {
        var now = DateTimeOffset.UtcNow;

        var challenge = AuthenticationChallengePolicy.Default
            .CreateEmailLoginChallenge("user-1", "test@example.com", now);

        challenge.Purpose.Should().Be(AuthenticationChallengePurpose.EmailLogin);
        challenge.Destination.Should().Be("test@example.com");
        challenge.TokenPurpose.Should().Be($"Authentication:EmailLogin:{challenge.Id.Value}");
    }

    [Fact]
    public void Challenges_with_the_same_user_and_purpose_get_distinct_token_purposes()
    {
        var now = DateTimeOffset.UtcNow;

        var first = AuthenticationChallengePolicy.Default
            .CreateEmailChangeChallenge("user-1", "same@example.com", now);
        var second = AuthenticationChallengePolicy.Default
            .CreateEmailChangeChallenge("user-1", "same@example.com", now);

        first.Id.Should().NotBe(second.Id);
        first.TokenPurpose.Should().NotBe(second.TokenPurpose);
    }

    [Fact]
    public void CreatePasskeyLoginChallenge_creates_challenge_with_correct_purpose()
    {
        var now = DateTimeOffset.UtcNow;

        var challenge = AuthenticationChallengePolicy.Default
            .CreatePasskeyLoginChallenge("user-1", "challenge-ref", now);

        challenge.Purpose.Should().Be(AuthenticationChallengePurpose.PasskeyLogin);
        challenge.Destination.Should().Be("challenge-ref");
    }

    [Fact]
    public void CreatePasswordLoginOtpChallenge_preserves_profile_state()
    {
        var challenge = AuthenticationChallengePolicy.Default.CreatePasswordLoginOtpChallenge(
            "user-1", "+1234567890", DateTimeOffset.UtcNow, requiresProfileCompletion: true);

        challenge.Purpose.Should().Be(AuthenticationChallengePurpose.PasswordLoginOtp);
        challenge.RequiresProfileCompletion.Should().BeTrue();
    }

    [Fact]
    public void CreatePasswordLoginOtpChallenge_allows_a_single_second_factor_attempt()
    {
        var challenge = AuthenticationChallengePolicy.Default.WithMaximumAttempts(5).CreatePasswordLoginOtpChallenge(
            "user-1", "+1234567890", DateTimeOffset.UtcNow, requiresProfileCompletion: false);

        challenge.IsSecondFactor.Should().BeTrue();
        challenge.MaximumAttempts.Should().Be(1);
    }

    [Fact]
    public void Single_factor_code_challenges_keep_the_configured_attempts()
    {
        var challenge = AuthenticationChallengePolicy.Default.WithMaximumAttempts(5).CreatePhoneLoginChallenge(
            "user-1", "+1234567890", DateTimeOffset.UtcNow);

        challenge.IsSecondFactor.Should().BeFalse();
        challenge.MaximumAttempts.Should().Be(5);
    }
}
