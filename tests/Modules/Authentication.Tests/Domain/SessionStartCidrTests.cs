using System.Net;
using FluentAssertions;
using Vitarish.Authentication.Domain.Security;

namespace Authentication.Tests.Domain;

public sealed class SessionStartCidrTests
{
    [Theory]
    [InlineData("10.0.0.7/24", "10.0.0.0/24")]
    [InlineData("2001:db8::7/64", "2001:db8::/64")]
    public void Normalizes_network_address(string input, string expected)
    {
        SessionStartCidr.TryParse(input, out string? normalized).Should().BeTrue();
        normalized.Should().Be(expected);
    }

    [Fact]
    public void Contains_addresses_inside_and_outside_the_network()
    {
        SessionStartCidr.Contains("10.0.0.0/24", IPAddress.Parse("10.0.0.200")).Should().BeTrue();
        SessionStartCidr.Contains("10.0.0.0/24", IPAddress.Parse("10.0.1.1")).Should().BeFalse();
        SessionStartCidr.Contains("2001:db8::/64", IPAddress.Parse("2001:db8::1")).Should().BeTrue();
        SessionStartCidr.Contains("2001:db8::/64", IPAddress.Parse("2001:db9::1")).Should().BeFalse();
        SessionStartCidr.Contains("0.0.0.0/0", IPAddress.Parse("192.0.2.1")).Should().BeTrue();
        SessionStartCidr.Contains("10.0.0.0/24", IPAddress.Parse("::ffff:10.0.0.7")).Should().BeFalse();
    }

    [Theory]
    [InlineData("10.0.0.7/024", "10.0.0.0/24")]
    [InlineData("2001:db8:0:0::7/064", "2001:db8::/64")]
    public void Canonicalizes_prefix_and_network_text(string input, string expected)
    {
        SessionStartCidr.TryParse(input, out string? normalized).Should().BeTrue();
        normalized.Should().Be(expected);
    }

    [Theory]
    [InlineData("10.0.0.0")]
    [InlineData("10.0.0.0/33")]
    [InlineData("2001:db8::/129")]
    [InlineData("2001:db8::/-1")]
    [InlineData("10.0.0.0/24/1")]
    [InlineData("not-a-cidr")]
    public void Rejects_non_cidr_values(string input)
    {
        SessionStartCidr.TryParse(input, out _).Should().BeFalse();
    }

    [Fact]
    public void NormalizeSet_rejects_duplicate_networks_after_canonicalization()
    {
        Action normalize = () => SessionStartCidr.NormalizeSet(["10.0.0.0/24", "10.0.0.7/24"]);

        normalize.Should().Throw<ArgumentException>();
    }
}
