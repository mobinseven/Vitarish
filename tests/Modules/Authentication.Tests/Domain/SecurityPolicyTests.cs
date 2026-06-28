using FluentAssertions;
using Vitarish.Authentication.Domain.Security;

namespace Authentication.Tests.Domain;

public class SecurityPolicyTests
{
    [Fact]
    public void LoginAttemptPolicy_exhausted_at_threshold()
    {
        var policy = new LoginAttemptPolicy(5);

        policy.IsExhausted(5).Should().BeTrue();
    }

    [Fact]
    public void LoginAttemptPolicy_not_exhausted_below_threshold()
    {
        var policy = new LoginAttemptPolicy(5);

        policy.IsExhausted(4).Should().BeFalse();
    }

    [Fact]
    public void LoginAttemptPolicy_exhausted_above_threshold()
    {
        var policy = new LoginAttemptPolicy(5);

        policy.IsExhausted(6).Should().BeTrue();
    }

    [Fact]
    public void LoginAttemptPolicy_not_exhausted_at_zero()
    {
        var policy = new LoginAttemptPolicy(5);

        policy.IsExhausted(0).Should().BeFalse();
    }

    [Fact]
    public void VerificationCodePolicy_valid_format()
    {
        var policy = new VerificationCodePolicy(6);

        policy.IsValidFormat("123456").Should().BeTrue();
    }

    [Fact]
    public void VerificationCodePolicy_rejects_wrong_length()
    {
        var policy = new VerificationCodePolicy(6);

        policy.IsValidFormat("12345").Should().BeFalse();
    }

    [Fact]
    public void VerificationCodePolicy_rejects_non_numeric()
    {
        var policy = new VerificationCodePolicy(6);

        policy.IsValidFormat("12ab34").Should().BeFalse();
    }

    [Fact]
    public void VerificationCodePolicy_rejects_null()
    {
        var policy = new VerificationCodePolicy(6);

        policy.IsValidFormat(null).Should().BeFalse();
    }

    [Fact]
    public void VerificationCodePolicy_rejects_empty()
    {
        var policy = new VerificationCodePolicy(6);

        policy.IsValidFormat("").Should().BeFalse();
    }

    [Fact]
    public void VerificationCodePolicy_rejects_whitespace()
    {
        var policy = new VerificationCodePolicy(6);

        policy.IsValidFormat("      ").Should().BeFalse();
    }
}
