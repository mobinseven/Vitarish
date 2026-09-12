using System.Net;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using SystemIPNetwork = System.Net.IPNetwork;
using Vitarish.Modules.Base.Core;
using Vitarish.Audit.Server;

namespace Vitarish.Hosting.Infrastructure.Options;

public sealed class SecurityConfigurationOptions
{
    public const string SectionName = "Vitarish:Security";
    public const string IdentityAuthorityKey = "Vitarish:Identity:Authority";
    public const string EvaluatedConfigurationSectionName = Module.EvaluatedConfigurationSection;
    public const string AllowedCorsOriginsSectionName = "Cors:AllowedOrigins";

    public string IdentityAuthority { get; init; } = string.Empty;
    public string[] AllowedCorsOrigins { get; init; } = [];
    public bool MandatoryAuditEnabled { get; init; }
    public string[] TrustedProxyNetworks { get; init; } = [];
    public TokenValidationConfiguration TokenValidation { get; init; } = new();
    public TlsConfiguration Tls { get; init; } = new();
    public DataProtectionConfiguration DataProtection { get; init; } = new();
    public EvaluatedConfigurationIdentity EvaluatedConfiguration { get; init; } = new();

    internal IReadOnlyList<SystemIPNetwork> ParseTrustedProxyNetworks()
    {
        if (TrustedProxyNetworks.Distinct(StringComparer.OrdinalIgnoreCase).Count() != TrustedProxyNetworks.Length)
            throw new InvalidOperationException("Security:TrustedProxyNetworks must not contain duplicate networks.");

        return TrustedProxyNetworks.Select(ParseNetwork).ToArray();
    }

    private static SystemIPNetwork ParseNetwork(string value)
    {
        string[] parts = value.Split('/', 2, StringSplitOptions.TrimEntries);
        if (!IPAddress.TryParse(parts[0], out IPAddress? address))
            throw new InvalidOperationException("Security:TrustedProxyNetworks contains an invalid network.");

        int prefix = address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork ? 32 : 128;
        if (parts.Length == 2 && (!int.TryParse(parts[1], out prefix) || prefix < 0 || prefix > (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork ? 32 : 128)))
            throw new InvalidOperationException("Security:TrustedProxyNetworks contains an invalid prefix.");

        if ((prefix == 0) || (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && address.Equals(IPAddress.Any)) ||
            (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6 && address.Equals(IPAddress.IPv6Any)))
            throw new InvalidOperationException("Security:TrustedProxyNetworks must not trust the entire address space.");

        return new SystemIPNetwork(address, prefix);
    }
}

public sealed class TokenValidationConfiguration
{
    public string Issuer { get; init; } = string.Empty;
    public string[] Audiences { get; init; } = [];
}

public sealed class TlsConfiguration
{
    public bool RequireHttps { get; init; } = true;
}

public sealed class DataProtectionConfiguration
{
    public string KeyRingRedisKey { get; init; } = string.Empty;
    public string CertificateRedisKey { get; init; } = string.Empty;
    public string[] PreviousCertificateRedisKeys { get; init; } = [];
}

public sealed class EvaluatedConfigurationIdentity
{
    public bool Enabled { get; init; }
    public string Identity { get; init; } = string.Empty;
    public string ManifestIdentity { get; init; } = string.Empty;
}

public static class SecurityConfigurationValidator
{
    public static SecurityConfigurationOptions Validate(IConfiguration configuration, IHostEnvironment environment, bool openApiGeneration = false)
    {
        bool strict = environment.IsProduction() || configuration.GetValue<bool>($"{SecurityConfigurationOptions.EvaluatedConfigurationSectionName}:Enabled");
        string authority = configuration[SecurityConfigurationOptions.IdentityAuthorityKey] ?? string.Empty;
        SecurityConfigurationOptions configured = configuration.GetSection(SecurityConfigurationOptions.SectionName).Get<SecurityConfigurationOptions>() ?? new();
        EvaluatedConfigurationIdentity evaluated = configuration.GetSection(SecurityConfigurationOptions.EvaluatedConfigurationSectionName).Get<EvaluatedConfigurationIdentity>() ?? configured.EvaluatedConfiguration;
        string[] origins = configuration.GetSection(SecurityConfigurationOptions.AllowedCorsOriginsSectionName).Get<string[]>() ?? [];

        if (strict && configured.MandatoryAuditEnabled is false)
            Fail("Security:MandatoryAuditEnabled must be true.");
        if (origins.Any(origin => origin == "*"))
            Fail("Cors:AllowedOrigins must contain exact origins when credentials are enabled.");
        if (origins.Any(origin => !Uri.TryCreate(origin, UriKind.Absolute, out Uri? uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) || uri.AbsolutePath != "/" ||
            !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment)))
            Fail("Cors:AllowedOrigins contains an invalid origin.");
        if (strict && origins.Length == 0)
            Fail("Cors:AllowedOrigins must contain at least one exact origin.");
        if (!Uri.TryCreate(authority, UriKind.Absolute, out Uri? authorityUri) || authorityUri.AbsolutePath != "/" || (strict && authorityUri.Scheme != Uri.UriSchemeHttps))
            Fail("Identity authority must be an absolute HTTPS authority.");
        if (configured.Tls.RequireHttps is false && strict)
            Fail("Security:Tls:RequireHttps cannot be disabled for production.");
        if (configured.TokenValidation.Audiences.Length == 0 && strict)
            Fail("Security:TokenValidation:Audiences must contain at least one audience.");
        if (string.IsNullOrWhiteSpace(configured.TokenValidation.Issuer) && strict)
            Fail("Security:TokenValidation:Issuer is required.");
        if (!string.IsNullOrWhiteSpace(configured.TokenValidation.Issuer) &&
            (!Uri.TryCreate(configured.TokenValidation.Issuer, UriKind.Absolute, out Uri? issuerUri) || issuerUri.AbsolutePath != "/" ||
             (strict && issuerUri.Scheme != Uri.UriSchemeHttps)))
            Fail("Security:TokenValidation:Issuer must be an absolute HTTPS authority.");
        if (configured.TokenValidation.Audiences.Any(string.IsNullOrWhiteSpace) ||
            configured.TokenValidation.Audiences.Distinct(StringComparer.Ordinal).Count() != configured.TokenValidation.Audiences.Length)
            Fail("Security:TokenValidation:Audiences must contain unique non-empty values.");
        if (strict && configured.TrustedProxyNetworks.Length == 0)
            Fail("Security:TrustedProxyNetworks must identify the local ingress networks.");
        if (configured.TrustedProxyNetworks.Length > 0)
            _ = configured.ParseTrustedProxyNetworks();
        if (strict && (string.IsNullOrWhiteSpace(configured.DataProtection.KeyRingRedisKey) || string.IsNullOrWhiteSpace(configured.DataProtection.CertificateRedisKey)))
            Fail("Security:DataProtection key locations are required.");
        ValidateKeyName(configured.DataProtection.KeyRingRedisKey);
        ValidateKeyName(configured.DataProtection.CertificateRedisKey);
        foreach (string key in configured.DataProtection.PreviousCertificateRedisKeys)
            ValidateKeyName(key);
        if (evaluated.Enabled && (string.IsNullOrWhiteSpace(evaluated.Identity) ||
            !string.Equals(evaluated.Identity, evaluated.ManifestIdentity, StringComparison.Ordinal)))
            Fail("Security:EvaluatedConfiguration identity must match the deployment manifest.");

        if (openApiGeneration && evaluated.Enabled)
            Fail("OpenAPI generation cannot use evaluated production configuration.");

        string? capacityWarningBytes = configuration[SecurityAuditOptions.CapacityWarningBytesKey];
        if (strict && (!long.TryParse(capacityWarningBytes, out long capacity) || capacity <= 0))
            Fail($"{SecurityAuditOptions.CapacityWarningBytesKey} must be a positive explicitly supplied value.");

        return new SecurityConfigurationOptions
        {
            MandatoryAuditEnabled = configured.MandatoryAuditEnabled,
            TrustedProxyNetworks = configured.TrustedProxyNetworks,
            TokenValidation = new TokenValidationConfiguration
            {
                Issuer = string.IsNullOrWhiteSpace(configured.TokenValidation.Issuer) ? authority : configured.TokenValidation.Issuer,
                Audiences = configured.TokenValidation.Audiences.Length == 0 ? ["vitarish-api"] : configured.TokenValidation.Audiences
            },
            DataProtection = new DataProtectionConfiguration
            {
                KeyRingRedisKey = configured.DataProtection.KeyRingRedisKey,
                CertificateRedisKey = configured.DataProtection.CertificateRedisKey,
                PreviousCertificateRedisKeys = configured.DataProtection.PreviousCertificateRedisKeys
            },
            Tls = configured.Tls,
            IdentityAuthority = authority,
            AllowedCorsOrigins = origins,
            EvaluatedConfiguration = evaluated
        };
    }

    private static void Fail(string rule) => throw new InvalidOperationException($"Security configuration validation failed: {rule}");

    private static void ValidateKeyName(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return;

        if (key.Contains('{') || key.Contains('}'))
        {
            string withoutTemplate = key.Replace("{domain}", string.Empty, StringComparison.Ordinal);
            if (withoutTemplate.Contains('{') || withoutTemplate.Contains('}'))
                Fail("Security:DataProtection key names may use only the {domain} template.");
        }
    }
}
