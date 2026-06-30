using FluentAssertions;
using Vitarish.Authentication.Domain.ReturnUrls;

namespace Authentication.Tests.Domain;

public class ReturnUrlPolicyTests
{
    [Theory]
    [InlineData("/dashboard")]
    [InlineData("/settings")]
    [InlineData("/a")]
    [InlineData("/")]
    public void IsSafeLocal_accepts_safe_local_paths(string url)
    {
        ReturnUrlPolicy.IsSafeLocal(url).Should().BeTrue();
    }

    [Theory]
    [InlineData("//evil.com")]
    [InlineData("/\\evil.com")]
    [InlineData("https://evil.com")]
    [InlineData("http://evil.com")]
    [InlineData("javascript:alert(1)")]
    [InlineData("/%5cevil.com")]
    [InlineData("%2f%2fevil.com")]
    public void IsSafeLocal_rejects_dangerous_urls(string url)
    {
        ReturnUrlPolicy.IsSafeLocal(url).Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void IsSafeLocal_returns_true_for_null_empty_whitespace(string? url)
    {
        ReturnUrlPolicy.IsSafeLocal(url).Should().BeTrue();
    }

    [Fact]
    public void NormalizeOrDefault_returns_url_when_safe()
    {
        var result = ReturnUrlPolicy.NormalizeOrDefault("/dashboard");

        result.Value.Should().Be("/dashboard");
    }

    [Fact]
    public void NormalizeOrDefault_returns_fallback_for_null()
    {
        var result = ReturnUrlPolicy.NormalizeOrDefault(null);

        result.Value.Should().Be("/");
    }

    [Fact]
    public void NormalizeOrDefault_returns_fallback_for_external_url()
    {
        var result = ReturnUrlPolicy.NormalizeOrDefault("https://evil.com");

        result.Value.Should().Be("/");
    }

    [Fact]
    public void NormalizeOrDefault_returns_custom_fallback()
    {
        var result = ReturnUrlPolicy.NormalizeOrDefault(null, "/custom");

        result.Value.Should().Be("/custom");
    }
}
