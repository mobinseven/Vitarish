using Microsoft.Extensions.Configuration;
using Vitarish.Accounts.Domain.Options;

namespace Vitarish.Accounts.Domain.Tests.Options;

public class AaasOptionsTests
{
    [Fact]
    public void AaasOptions_DefaultValues_AreExpected()
    {
        AaasOptions options = new()
        {
            Authority = "https://auth.example.com",
            Secret = "secret",
            ClientId = "client-id"
        };

        options.CookieExpirationDays.Should().Be(14);
        options.Local.Should().BeNull();
        options.Dev.Should().BeNull();
        options.DataProtectionDomain.Should().BeNull();
        options.DataProtectionPassword.Should().BeNull();
    }

    [Fact]
    public void AaasOptions_BindsFromConfiguration()
    {
        Dictionary<string, string?> settings = new()
        {
            ["Aaas:Authority"] = "https://auth.example.com",
            ["Aaas:Secret"] = "secret",
            ["Aaas:ClientId"] = "client-id",
            ["Aaas:Local"] = "true",
            ["Aaas:Dev"] = "false",
            ["Aaas:DataProtectionDomain"] = "dp-domain",
            ["Aaas:DataProtectionPassword"] = "dp-password",
            ["Aaas:CookieExpirationDays"] = "21"
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        AaasOptions options = new()
        {
            Authority = string.Empty,
            Secret = string.Empty,
            ClientId = string.Empty
        };
        configuration.GetSection("Aaas").Bind(options);

        options.Authority.Should().Be("https://auth.example.com");
        options.Secret.Should().Be("secret");
        options.ClientId.Should().Be("client-id");
        options.Local.Should().BeTrue();
        options.Dev.Should().BeFalse();
        options.DataProtectionDomain.Should().Be("dp-domain");
        options.DataProtectionPassword.Should().Be("dp-password");
        options.CookieExpirationDays.Should().Be(21);
    }
}
