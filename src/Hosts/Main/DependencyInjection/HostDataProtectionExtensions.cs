using Microsoft.AspNetCore.DataProtection;
using StackExchange.Redis;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Vitarish.Main.DependencyInjection;

public static class HostDataProtectionExtensions
{
    private const string ConfigSection = $"{nameof(Vitarish)}:Identity";

    public static async Task AddHostDataProtection(this IServiceCollection services, IConfiguration configuration)
    {
        string domain = configuration[$"{ConfigSection}:Authority"] ?? throw new Exception($"{ConfigSection}:Authority not set");
        string pfxPassword = configuration[$"{ConfigSection}:DataProtectionPassword"] ?? throw new Exception($"{ConfigSection}:DataProtectionPassword not set");
        ConnectionMultiplexer redis = ConnectionMultiplexer.Connect(configuration.GetRedisConnectionString());
        X509Certificate2 cert = await GetOrCreateFromRedisAsync(redis, domain, pfxPassword);
        _ = services
            .AddDataProtection()
            .PersistKeysToStackExchangeRedis(redis, "DataProtectionKeys")
            .ProtectKeysWithCertificate(cert)
            .SetApplicationName(domain);
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

        using RSA rsa = RSA.Create(keySize);
        X500DistinguishedName dn = new($"CN={dnsName}");

        CertificateRequest req = new(
            dn, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        req.CertificateExtensions.Add(
            new X509KeyUsageExtension(
                X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, true));
        req.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(req.PublicKey, false));

        SubjectAlternativeNameBuilder san = new();
        san.AddDnsName(dnsName);
        req.CertificateExtensions.Add(san.Build());

        DateTimeOffset notBefore = DateTimeOffset.UtcNow.AddMinutes(-5);
        DateTimeOffset notAfter = DateTimeOffset.UtcNow.AddYears(yearsValid);
        using X509Certificate2 created = req.CreateSelfSigned(notBefore, notAfter);

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
}
