using FluentAssertions;
using Vitarish.Parties.Domain.Addresses;

namespace Vitarish.Parties.Tests.Domain;

public sealed class TenantSubdomainTests
{
    [Theory]
    [InlineData("abc")]
    [InlineData("dr-rezaei")]
    [InlineData("clinic42")]
    [InlineData("123")]
    [InlineData("abcdefghijklmnopqrstuvwxyz1234")]
    public void Accepts_lowercase_letters_digits_and_inner_hyphens_of_3_to_30_characters(string candidate)
    {
        TenantSubdomain.TryCreate(candidate, out TenantSubdomain? subdomain).Should().Be(TenantSubdomainRejection.None);
        subdomain!.Value.Should().Be(candidate);
    }

    [Fact]
    public void Trims_and_lowercases_before_applying_the_rules()
    {
        TenantSubdomain.TryCreate("  Dr-Rezaei ", out TenantSubdomain? subdomain).Should().Be(TenantSubdomainRejection.None);
        subdomain!.Value.Should().Be("dr-rezaei");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("ab")]
    [InlineData("abcdefghijklmnopqrstuvwxyz12345")]
    [InlineData("-abc")]
    [InlineData("abc-")]
    [InlineData("ab--cd")]
    [InlineData("xn--abc")]
    [InlineData("dr_rezaei")]
    [InlineData("dr.rezaei")]
    [InlineData("dr rezaei")]
    [InlineData("مطب")]
    [InlineData("café")]
    public void Rejects_malformed_subdomains(string? candidate)
    {
        TenantSubdomain.TryCreate(candidate, out TenantSubdomain? subdomain).Should().Be(TenantSubdomainRejection.Malformed);
        subdomain.Should().BeNull();
    }

    [Theory]
    [InlineData("www")]
    [InlineData("admin")]
    [InlineData("API")]
    [InlineData("start")]
    [InlineData("clinic")]
    [InlineData("patients")]
    [InlineData("mail")]
    public void Rejects_reserved_subdomains_in_any_case(string candidate)
    {
        TenantSubdomain.TryCreate(candidate, out TenantSubdomain? subdomain).Should().Be(TenantSubdomainRejection.Reserved);
        subdomain.Should().BeNull();
    }

    [Theory]
    [InlineData("darman.vitarish.com")]
    [InlineData("Darman.Vitarish.com.")]
    [InlineData(" darman.vitarish.com ")]
    public void Builds_the_hostname_under_a_normalized_base_domain(string baseDomain)
    {
        TenantSubdomain.TryCreate("rezaei", out TenantSubdomain? subdomain);

        subdomain!.ToHostname(baseDomain).Should().Be("rezaei.darman.vitarish.com");
    }
}
