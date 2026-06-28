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
    public void TokenPurpose_returns_phone_login_purpose()
    {
        AuthenticationChallengePolicy.Default.TokenPurpose(AuthenticationChallengePurpose.PhoneLogin)
            .Should().Be("Authentication:PhoneLogin");
    }

    [Fact]
    public void TokenPurpose_returns_email_login_purpose()
    {
        AuthenticationChallengePolicy.Default.TokenPurpose(AuthenticationChallengePurpose.EmailLogin)
            .Should().Be("Authentication:EmailLogin");
    }

    [Fact]
    public void TokenPurpose_returns_passkey_purpose()
    {
        AuthenticationChallengePolicy.Default.TokenPurpose(AuthenticationChallengePurpose.PasskeyLogin)
            .Should().Be("Authentication:PasskeyLogin");
    }

    [Fact]
    public void CreatePhoneLoginChallenge_creates_challenge_with_correct_purpose()
    {
        var now = DateTimeOffset.UtcNow;

        var challenge = AuthenticationChallengePolicy.Default
            .CreatePhoneLoginChallenge("user-1", "+1234567890", "token-purpose", now);

        challenge.Purpose.Should().Be(AuthenticationChallengePurpose.PhoneLogin);
        challenge.UserId.Should().Be("user-1");
        challenge.Destination.Should().Be("+1234567890");
        challenge.CreatedAt.Should().Be(now);
        challenge.ExpiresAt.Should().Be(now.AddMinutes(5));
        challenge.MaximumAttempts.Should().Be(5);
    }

    [Fact]
    public void CreateEmailLoginChallenge_creates_challenge_with_correct_purpose()
    {
        var now = DateTimeOffset.UtcNow;

        var challenge = AuthenticationChallengePolicy.Default
            .CreateEmailLoginChallenge("user-1", "test@example.com", "token-purpose", now);

        challenge.Purpose.Should().Be(AuthenticationChallengePurpose.EmailLogin);
        challenge.Destination.Should().Be("test@example.com");
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
}
