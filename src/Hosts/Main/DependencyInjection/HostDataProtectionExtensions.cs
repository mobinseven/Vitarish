using Microsoft.AspNetCore.DataProtection;
using StackExchange.Redis;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using HostDataProtectionOptions = Vitarish.Main.Infrastructure.Options.DataProtectionOptions;

namespace Vitarish.Main.DependencyInjection;

public static class HostDataProtectionExtensions
{
    private const string ConfigSection = $"{nameof(Vitarish)}:Identity";

    public static async Task AddHostDataProtection(this IServiceCollection services, IConfiguration configuration)
    {
        string domain = configuration[$"{ConfigSection}:Authority"] ?? throw new Exception($"{ConfigSection}:Authority not set");
        string pfxPassword = configuration[$"{ConfigSection}:DataProtectionPassword"] ?? throw new Exception($"{ConfigSection}:DataProtectionPassword not set");
        HostDataProtectionOptions options = configuration.GetSection($"{ConfigSection}:DataProtection").Get<HostDataProtectionOptions>()
            ?? throw new Exception($"{ConfigSection}:DataProtection not set");
        if (string.IsNullOrWhiteSpace(options.KeyRingRedisKey) || string.IsNullOrWhiteSpace(options.CertificateRedisKey))
            throw new Exception($"{ConfigSection}:DataProtection key names must be configured");
        ConnectionMultiplexer redis = await ConnectionMultiplexer.ConnectAsync(configuration.GetRedisConnectionString());
        string certificateKey = options.CertificateRedisKey.Replace("{domain}", domain, StringComparison.Ordinal);
        X509Certificate2 cert = await GetOrCreateFromRedisAsync(redis, domain, pfxPassword, certificateKey);
        IDataProtectionBuilder dataProtection = services
            .AddDataProtection()
            .PersistKeysToStackExchangeRedis(redis, options.KeyRingRedisKey)
            .ProtectKeysWithCertificate(cert)
            .SetApplicationName(domain);
        X509Certificate2[] previousCertificates = await LoadCertificatesAsync(redis,
            options.PreviousCertificateRedisKeys.Select(key => key.Replace("{domain}", domain, StringComparison.Ordinal)), pfxPassword);
        if (previousCertificates.Length > 0)
            _ = dataProtection.UnprotectKeysWithAnyCertificate(previousCertificates);
    }

    public static void AddOpenApiDataProtection(this IServiceCollection services, IConfiguration configuration)
    {
        string domain = configuration[$"{ConfigSection}:Authority"] ?? throw new Exception($"{ConfigSection}:Authority not set");
        string pfxPassword = configuration[$"{ConfigSection}:DataProtectionPassword"] ?? throw new Exception($"{ConfigSection}:DataProtectionPassword not set");
        X509Certificate2 certificate = CreateOpenApiCertificate(domain, pfxPassword);
        _ = services.AddDataProtection()
            .ProtectKeysWithCertificate(certificate)
            .SetApplicationName(domain);
    }

    private static async Task<X509Certificate2[]> LoadCertificatesAsync(
        IConnectionMultiplexer redis,
        IEnumerable<string> keys,
        string password)
    {
        IDatabase db = redis.GetDatabase();
        List<X509Certificate2> certificates = [];
        foreach (string key in keys.Where(key => !string.IsNullOrWhiteSpace(key)))
        {
            RedisValue stored = await db.StringGetAsync(key);
            if (!stored.HasValue)
                throw new InvalidOperationException($"Configured Data Protection certificate key '{key}' was not found.");
            certificates.Add(X509CertificateLoader.LoadPkcs12((byte[])stored!, password,
                X509KeyStorageFlags.EphemeralKeySet | X509KeyStorageFlags.Exportable));
        }
        return [.. certificates];
    }

    public static Task AddHostDataProtection(this WebApplicationBuilder builder) => AddHostDataProtection(builder.Services, builder.Configuration);

    public static async Task<X509Certificate2> GetOrCreateFromRedisAsync(
        IConnectionMultiplexer mux,
        string dnsName,
        string pfxPassword,
        string? redisKey = null,
        TimeSpan? ttl = null,
        int keySize = 2048,
        int yearsValid = 1)
    {
        IDatabase db = mux.GetDatabase();
        redisKey ??= $"cert:pfx:{dnsName}";
        RedisValue stored = await db.StringGetAsync(redisKey);
        if (stored.HasValue)
        {
            byte[] pfxBytes = (byte[])stored!;
            return X509CertificateLoader.LoadPkcs12(
                pfxBytes, pfxPassword,
                X509KeyStorageFlags.EphemeralKeySet | X509KeyStorageFlags.Exportable);
        }

        using X509Certificate2 created = CreateOpenApiCertificate(dnsName, pfxPassword, keySize, yearsValid);

        byte[] pfx = created.Export(X509ContentType.Pfx, pfxPassword);
        _ = await db.StringSetAsync(redisKey, pfx);
        if (ttl.HasValue)
        {
            _ = await db.KeyExpireAsync(redisKey, ttl);
        }

        return X509CertificateLoader.LoadPkcs12(
            pfx, pfxPassword,
            X509KeyStorageFlags.EphemeralKeySet | X509KeyStorageFlags.Exportable);
    }

    public static X509Certificate2 CreateOpenApiCertificate(
        string dnsName,
        string password,
        int keySize = 2048,
        int yearsValid = 1)
    {
        using RSA rsa = RSA.Create(keySize);
        CertificateRequest request = new(
            new X500DistinguishedName($"CN={dnsName}"),
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509KeyUsageExtension(
            X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, true));
        request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));
        SubjectAlternativeNameBuilder san = new();
        san.AddDnsName(dnsName);
        request.CertificateExtensions.Add(san.Build());
        using X509Certificate2 created = request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddYears(yearsValid));
        byte[] pfx = created.Export(X509ContentType.Pfx, password);
        return X509CertificateLoader.LoadPkcs12(
            pfx, password,
            X509KeyStorageFlags.EphemeralKeySet | X509KeyStorageFlags.Exportable);
    }
}
