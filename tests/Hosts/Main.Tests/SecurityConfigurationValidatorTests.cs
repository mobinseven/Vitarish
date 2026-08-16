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

    [Fact]
    public void Evaluated_configuration_requires_manifest_identity_match()
    {
        IConfiguration configuration = Configuration([
            new("Vitarish:EvaluatedConfiguration:Enabled", "true"),
            new("Vitarish:EvaluatedConfiguration:Identity", "release-a"),
            new("Vitarish:EvaluatedConfiguration:ManifestIdentity", "release-b"),
            new("Vitarish:Security:MandatoryAuditEnabled", "true"),
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
    public void Legacy_previous_certificate_keys_are_preserved_when_new_section_is_empty()
    {
        IConfiguration configuration = Configuration([
            new("Vitarish:Identity:Authority", "https://issuer.example"),
            new("Vitarish:Identity:DataProtection:PreviousCertificateRedisKeys:0", "cert:previous:{domain}"),
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

    private static IHostEnvironment Environment(string name) => new HostingEnvironment { EnvironmentName = name };

    private sealed class HostingEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = string.Empty;
        public string ApplicationName { get; set; } = "Main.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
