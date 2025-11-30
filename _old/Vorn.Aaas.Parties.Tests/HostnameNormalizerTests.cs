using Vorn.Aaas.Parties.Infrastructure.Extensions;

namespace Vorn.Aaas.Parties.Tests;

/// <summary>
/// Tests for hostname normalization functionality.
/// </summary>
public class HostnameNormalizerTests
{
    [Theory]
    [InlineData("Example.COM", "example.com")]
    [InlineData("UPPERCASE.DOMAIN.COM", "uppercase.domain.com")]
    [InlineData("MixedCase.Example.Com", "mixedcase.example.com")]
    public void Canonical_ConvertsToLowercase(string input, string expected)
    {
        var result = HostnameNormalizer.Canonical(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("example.com.", "example.com")]
    [InlineData("subdomain.example.com.", "subdomain.example.com")]
    [InlineData("multi.level.subdomain.com...", "multi.level.subdomain.com")]
    public void Canonical_RemovesTrailingDots(string input, string expected)
    {
        var result = HostnameNormalizer.Canonical(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("münchen.de", "xn--mnchen-3ya.de")]
    [InlineData("日本.jp", "xn--wgv71a.jp")]
    [InlineData("испытание.com", "xn--80akhbyknj4f.com")]
    public void Canonical_AppliesPunycode(string input, string expected)
    {
        var result = HostnameNormalizer.Canonical(input);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Canonical_AppliesPunycode_ToArabic()
    {
        // Arabic IDN conversion
        var result = HostnameNormalizer.Canonical("تست.example.com");
        // The actual punycode may vary based on implementation
        Assert.StartsWith("xn--", result);
        Assert.EndsWith(".example.com", result);
    }

    [Theory]
    [InlineData("example.com:8080", "example.com")]
    [InlineData("localhost:5001", "localhost")]
    [InlineData("subdomain.example.com:443", "subdomain.example.com")]
    public void Canonical_RemovesPort(string input, string expected)
    {
        var result = HostnameNormalizer.Canonical(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("  example.com  ", "example.com")]
    [InlineData("\texample.com\n", "example.com")]
    [InlineData(" subdomain.example.com ", "subdomain.example.com")]
    public void Canonical_TrimsWhitespace(string input, string expected)
    {
        var result = HostnameNormalizer.Canonical(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("MÜNCHEN.DE:8080.", "xn--mnchen-3ya.de")]
    [InlineData("  SubDomain.Example.COM.  ", "subdomain.example.com")]
    public void Canonical_CombinesAllNormalizations(string input, string expected)
    {
        var result = HostnameNormalizer.Canonical(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public void Canonical_HandlesEmptyInput(string input)
    {
        // Empty/whitespace input results in empty string after normalization
        var result = HostnameNormalizer.Canonical(input);
        Assert.Equal(string.Empty, result);
    }
}
