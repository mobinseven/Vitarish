using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Vitarish.Hosting.Infrastructure.Options;
using Xunit;

namespace Main.Tests;

public sealed class SecurityConfigurationValidatorTests
{
    [Fact]
    public void Production_requires_explicit_trusted_proxy_networks()
    {
        IConfiguration configuration = Configuration([
            new("Vitarish:Security:MandatoryAuditEnabled", "true"),
            new("Cors:AllowedOrigins:0", "https://app.example"),
            new("Vitarish:Identity:Authority", "https://issuer.example"),
            new("Vitarish:Security:TokenValidation:Issuer", "https://issuer.example"),
            new("Vitarish:Security:TokenValidation:Audiences:0", "vitarish-api"),
            new("Vitarish:Security:DataProtection:KeyRingRedisKey", "keys"),
            new("Vitarish:Security:DataProtection:CertificateRedisKey", "cert")
        ]);

        Action act = () => SecurityConfigurationValidator.Validate(configuration, Environment("Production"));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Security:TrustedProxyNetworks*");
    }

    [Fact]
    public void Production_rejects_wildcard_cors_with_credentials()
    {
        IConfiguration configuration = Configuration([
            new("Vitarish:Security:MandatoryAuditEnabled", "true"),
            new("Cors:AllowedOrigins:0", "*"),
            new("Vitarish:Identity:Authority", "https://issuer.example"),
            new("Vitarish:Security:TokenValidation:Issuer", "https://issuer.example"),
            new("Vitarish:Security:TokenValidation:Audiences:0", "vitarish-api"),
            new("Vitarish:Security:DataProtection:KeyRingRedisKey", "keys"),
            new("Vitarish:Security:DataProtection:CertificateRedisKey", "cert")
        ]);

        Action act = () => SecurityConfigurationValidator.Validate(configuration, Environment("Production"));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cors:AllowedOrigins*");
    }

    [Theory]
    [InlineData("http://issuer.example", "Security:TokenValidation:Issuer")]
    [InlineData("not-an-authority", "Security:TokenValidation:Issuer")]
    public void Production_rejects_unsafe_or_malformed_token_issuer(string issuer, string expectedRule)
    {
        IConfiguration configuration = StrictConfiguration(("Vitarish:Security:TokenValidation:Issuer", issuer));

        Action act = () => SecurityConfigurationValidator.Validate(configuration, Environment("Production"));

        act.Should().Throw<InvalidOperationException>().WithMessage($"*{expectedRule}*");
    }

    [Fact]
    public void Production_rejects_duplicate_token_audiences_without_echoing_values()
    {
        IConfiguration configuration = StrictConfiguration(
            ("Vitarish:Security:TokenValidation:Audiences:1", "vitarish-api"));

        Action act = () => SecurityConfigurationValidator.Validate(configuration, Environment("Production"));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Audiences*")
            .Which.Message.Should().NotContain("vitarish-api");
    }

    [Fact]
    public void Production_rejects_invalid_data_protection_key_template()
    {
        IConfiguration configuration = StrictConfiguration(
            ("Vitarish:Security:DataProtection:CertificateRedisKey", "cert:{tenant}"));

        Action act = () => SecurityConfigurationValidator.Validate(configuration, Environment("Production"));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*DataProtection key names*");
    }

    [Theory]
    [InlineData("https://app.example/path?x=1")]
    [InlineData("https://app.example/#fragment")]
    public void Production_rejects_origins_with_path_query_or_fragment(string origin)
    {
        IConfiguration configuration = StrictConfiguration(("Cors:AllowedOrigins:0", origin));

        Action act = () => SecurityConfigurationValidator.Validate(configuration, Environment("Production"));

        act.Should().Throw<InvalidOperationException>().WithMessage("*AllowedOrigins contains an invalid origin*");
    }

    [Fact]
    public void Production_rejects_duplicate_proxy_networks()
    {
        IConfiguration configuration = StrictConfiguration(("Vitarish:Security:TrustedProxyNetworks:1", "127.0.0.1/32"));

        Action act = () => SecurityConfigurationValidator.Validate(configuration, Environment("Production"));

        act.Should().Throw<InvalidOperationException>().WithMessage("*duplicate networks*");
    }

    [Fact]
    public void Evaluated_configuration_requires_identity()
    {
        IConfiguration configuration = StrictConfiguration(
            ("Vitarish:EvaluatedConfiguration:Enabled", "true"),
            ("Vitarish:EvaluatedConfiguration:Identity", ""),
            ("Vitarish:EvaluatedConfiguration:ManifestIdentity", "release-a"));

        Action act = () => SecurityConfigurationValidator.Validate(configuration, Environment("Development"));

        act.Should().Throw<InvalidOperationException>().WithMessage("*EvaluatedConfiguration identity*");
    }

    [Fact]
    public void Evaluated_configuration_requires_manifest_identity_match()
    {
        IConfiguration configuration = Configuration([
            new("Vitarish:EvaluatedConfiguration:Enabled", "true"),
            new("Vitarish:EvaluatedConfiguration:Identity", "release-a"),
            new("Vitarish:EvaluatedConfiguration:ManifestIdentity", "release-b"),
            new("Vitarish:Security:MandatoryAuditEnabled", "true"),
            new("Cors:AllowedOrigins:0", "https://app.example"),
            new("Vitarish:Security:TrustedProxyNetworks:0", "127.0.0.1/32")
            ,new("Vitarish:Identity:Authority", "https://issuer.example"),
            new("Vitarish:Security:TokenValidation:Issuer", "https://issuer.example"),
            new("Vitarish:Security:TokenValidation:Audiences:0", "vitarish-api"),
            new("Vitarish:Security:DataProtection:KeyRingRedisKey", "keys"),
            new("Vitarish:Security:DataProtection:CertificateRedisKey", "cert")
        ]);

        Action act = () => SecurityConfigurationValidator.Validate(configuration, Environment("Development"));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*EvaluatedConfiguration identity*");
    }

    [Fact]
    public void Valid_production_configuration_returns_normalized_token_validation()
    {
        IConfiguration configuration = Configuration([
            new("Vitarish:Security:MandatoryAuditEnabled", "true"),
            new("Cors:AllowedOrigins:0", "https://app.example"),
            new("Vitarish:Security:TrustedProxyNetworks:0", "127.0.0.1/32"),
            new("Vitarish:Security:TokenValidation:Issuer", "https://issuer.example"),
            new("Vitarish:Security:TokenValidation:Audiences:0", "vitarish-api"),
            new("Vitarish:Security:DataProtection:KeyRingRedisKey", "keys"),
            new("Vitarish:Security:DataProtection:CertificateRedisKey", "cert:{domain}"),
            new("Vitarish:Identity:Authority", "https://issuer.example")
        ]);

        SecurityConfigurationOptions result = SecurityConfigurationValidator.Validate(configuration, Environment("Production"));

        result.TokenValidation.Issuer.Should().Be("https://issuer.example");
        result.TokenValidation.Audiences.Should().ContainSingle().Which.Should().Be("vitarish-api");
    }

    [Fact]
    public void OpenApi_generation_rejects_evaluated_configuration()
    {
        IConfiguration configuration = Configuration([
            new("Vitarish:EvaluatedConfiguration:Enabled", "true"),
            new("Vitarish:EvaluatedConfiguration:Identity", "release-a"),
            new("Vitarish:EvaluatedConfiguration:ManifestIdentity", "release-a"),
            new("Vitarish:Identity:Authority", "https://issuer.example"),
            new("Vitarish:Security:MandatoryAuditEnabled", "true"),
            new("Cors:AllowedOrigins:0", "https://app.example"),
            new("Vitarish:Security:TrustedProxyNetworks:0", "127.0.0.1/32"),
            new("Vitarish:Security:TokenValidation:Issuer", "https://issuer.example"),
            new("Vitarish:Security:TokenValidation:Audiences:0", "vitarish-api"),
            new("Vitarish:Security:DataProtection:KeyRingRedisKey", "keys"),
            new("Vitarish:Security:DataProtection:CertificateRedisKey", "cert")
        ]);

        Action act = () => SecurityConfigurationValidator.Validate(configuration, Environment("Development"), openApiGeneration: true);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*OpenAPI generation cannot use evaluated production configuration*");
    }

    [Fact]
    public void Previous_certificate_keys_are_read_from_the_security_section()
    {
        IConfiguration configuration = Configuration([
            new("Vitarish:Identity:Authority", "https://issuer.example"),
            new("Vitarish:Security:DataProtection:PreviousCertificateRedisKeys:0", "cert:previous:{domain}"),
            new("Vitarish:Security:TrustedProxyNetworks:0", "127.0.0.1/32"),
            new("Vitarish:Security:TokenValidation:Issuer", "https://issuer.example"),
            new("Vitarish:Security:TokenValidation:Audiences:0", "vitarish-api")
        ]);

        SecurityConfigurationOptions result = SecurityConfigurationValidator.Validate(configuration, Environment("Development"));

        result.DataProtection.PreviousCertificateRedisKeys.Should().ContainSingle()
            .Which.Should().Be("cert:previous:{domain}");
    }

    private static IConfiguration Configuration(IEnumerable<KeyValuePair<string, string?>> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    private static IConfiguration StrictConfiguration(params (string Key, string Value)[] overrides)
    {
        Dictionary<string, string?> values = new(StringComparer.Ordinal)
        {
            ["Vitarish:Security:MandatoryAuditEnabled"] = "true",
            ["Cors:AllowedOrigins:0"] = "https://app.example",
            ["Vitarish:Security:TrustedProxyNetworks:0"] = "127.0.0.1/32",
            ["Vitarish:Identity:Authority"] = "https://issuer.example",
            ["Vitarish:Security:TokenValidation:Issuer"] = "https://issuer.example",
            ["Vitarish:Security:TokenValidation:Audiences:0"] = "vitarish-api",
            ["Vitarish:Security:DataProtection:KeyRingRedisKey"] = "keys",
            ["Vitarish:Security:DataProtection:CertificateRedisKey"] = "cert:{domain}"
        };
        foreach ((string key, string value) in overrides)
            values[key] = value;
        return Configuration(values);
    }

    private static IHostEnvironment Environment(string name) => new HostingEnvironment { EnvironmentName = name };

    private sealed class HostingEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = string.Empty;
        public string ApplicationName { get; set; } = "Main.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
