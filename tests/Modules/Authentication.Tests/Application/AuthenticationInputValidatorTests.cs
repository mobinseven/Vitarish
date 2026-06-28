using FluentAssertions;
using Vitarish.Authentication.Application.Validation;

namespace Authentication.Tests.Application;

public class AuthenticationInputValidatorTests
{
    [Fact]
    public void HasText_returns_true_for_non_empty_string()
    {
        AuthenticationInputValidator.HasText("hello").Should().BeTrue();
    }

    [Fact]
    public void HasText_returns_false_for_null()
    {
        AuthenticationInputValidator.HasText(null).Should().BeFalse();
    }

    [Fact]
    public void HasText_returns_false_for_empty()
    {
        AuthenticationInputValidator.HasText("").Should().BeFalse();
    }

    [Fact]
    public void HasText_returns_false_for_whitespace()
    {
        AuthenticationInputValidator.HasText("   ").Should().BeFalse();
    }

    [Fact]
    public void SafeReturnUrl_returns_default_for_null()
    {
        AuthenticationInputValidator.SafeReturnUrl(null).Should().Be("/");
    }

    [Fact]
    public void SafeReturnUrl_returns_url_when_safe()
    {
        AuthenticationInputValidator.SafeReturnUrl("/dashboard").Should().Be("/dashboard");
    }

    [Fact]
    public void SafeReturnUrl_returns_default_for_external_url()
    {
        AuthenticationInputValidator.SafeReturnUrl("https://evil.com").Should().Be("/");
    }
}
