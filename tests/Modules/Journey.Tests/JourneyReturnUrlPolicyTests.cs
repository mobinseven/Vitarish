using Vitarish.Journey.Infrastructure.ReturnUrl;

namespace Journey.Tests;

public class JourneyReturnUrlPolicyTests
{
    private readonly JourneyReturnUrlPolicy _sut = new();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsSafe_NullOrBlank_ReturnsFalse(string? returnUrl)
    {
        _sut.IsSafe(returnUrl).Should().BeFalse();
    }

    [Theory]
    [InlineData("/dashboard")]
    [InlineData("/home")]
    [InlineData("/account/profile/complete")]
    [InlineData("/workspaces/select")]
    [InlineData("/apps/app1/dashboard")]
    public void IsSafe_ValidLocalPath_ReturnsTrue(string returnUrl)
    {
        _sut.IsSafe(returnUrl).Should().BeTrue();
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/login")]
    [InlineData("/login/password")]
    [InlineData("/logout")]
    [InlineData("/logout/callback")]
    [InlineData("/api/modules/authentication/login/password")]
    public void IsSafe_AuthenticationOnlyRoute_ReturnsFalse(string returnUrl)
    {
        _sut.IsSafe(returnUrl).Should().BeFalse();
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/login")]
    [InlineData("/login/password")]
    [InlineData("/logout")]
    [InlineData("/logout/callback")]
    [InlineData("/api/modules/authentication/login/password")]
    public void Normalize_AuthenticationOnlyRoute_ReturnsNull(string returnUrl)
    {
        _sut.Normalize(returnUrl).Should().BeNull();
    }

    [Theory]
    [InlineData("https://external.example")]
    [InlineData("http://external.example")]
    public void IsSafe_ExternalAbsoluteUrl_ReturnsFalse(string returnUrl)
    {
        _sut.IsSafe(returnUrl).Should().BeFalse();
    }

    [Theory]
    [InlineData("//external.example")]
    [InlineData("//evil.com")]
    public void IsSafe_ProtocolRelativeUrl_ReturnsFalse(string returnUrl)
    {
        _sut.IsSafe(returnUrl).Should().BeFalse();
    }

    [Theory]
    [InlineData("\\external.example")]
    [InlineData("/\\external.example")]
    public void IsSafe_BackslashRelativeUrl_ReturnsFalse(string returnUrl)
    {
        _sut.IsSafe(returnUrl).Should().BeFalse();
    }

    [Theory]
    [InlineData("/%2f%2fexternal.example")]
    [InlineData("/%5cexternal.example")]
    public void IsSafe_EncodedBypassAttempt_ReturnsFalse(string returnUrl)
    {
        _sut.IsSafe(returnUrl).Should().BeFalse();
    }

    [Theory]
    [InlineData("/path?query=value")]
    [InlineData("/path#fragment")]
    public void IsSafe_UrlWithQueryOrFragment_ReturnsFalse(string returnUrl)
    {
        _sut.IsSafe(returnUrl).Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Normalize_NullOrBlank_ReturnsNull(string? returnUrl)
    {
        _sut.Normalize(returnUrl).Should().BeNull();
    }

    [Theory]
    [InlineData("https://evil.com")]
    [InlineData("//evil.com")]
    [InlineData("\\evil.com")]
    public void Normalize_UnsafeUrl_ReturnsNull(string returnUrl)
    {
        _sut.Normalize(returnUrl).Should().BeNull();
    }

    [Theory]
    [InlineData("/dashboard", "/dashboard")]
    [InlineData("/home", "/home")]
    public void Normalize_SafeUrl_ReturnsSameUrl(string input, string expected)
    {
        _sut.Normalize(input).Should().Be(expected);
    }
}
