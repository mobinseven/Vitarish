using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
public static class AaasDataProtectionExtensions
{
    public static async Task AddAaasDataProtection(this IServiceCollection services, IConfiguration configuration)
    {
        string domain = configuration["Vorn:Aaas:Authority"] ?? throw new Exception("Vorn:Aaas:Authority not set");
        string pfxPassword = configuration["Vorn:Aaas:DataProtectionPassword"] ?? throw new Exception("Vorn:Aaas:DataProtectionPassword not set");
        ConnectionMultiplexer redis = ConnectionMultiplexer.Connect(configuration.GetRedisConnectionString());
        X509Certificate2 cert = await GetOrCreateFromRedisAsync(redis, domain, pfxPassword); // GenerateCertificate(domain, pfxPassword);
        _ = services
            .AddDataProtection()
            //.PersistKeysToFileSystem(new DirectoryInfo("Keys"))
            .PersistKeysToStackExchangeRedis(redis, "DataProtectionKeys")
            .ProtectKeysWithCertificate(cert)
            .SetApplicationName(domain);
    }
    public static async Task AddAaasDataProtection(this WebApplicationBuilder builder)
    {
        string domain = builder.Configuration["Vorn:Aaas:Authority"] ?? throw new Exception("Vorn:Aaas:Authority not set");
        string pfxPassword = builder.Configuration["Vorn:Aaas:DataProtectionPassword"] ?? throw new Exception("Vorn:Aaas:DataProtectionPassword not set");
        ConnectionMultiplexer redis = ConnectionMultiplexer.Connect(builder.Configuration.GetRedisConnectionString());
        X509Certificate2 cert = await GetOrCreateFromRedisAsync(redis, domain, pfxPassword); // GenerateCertificate(domain, pfxPassword);
        _ = builder.Services
            .AddDataProtection()
            //.PersistKeysToFileSystem(new DirectoryInfo("Keys"))
            .PersistKeysToStackExchangeRedis(redis, "DataProtectionKeys")
            .ProtectKeysWithCertificate(cert)
            .SetApplicationName(domain);
    }


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
        // 1) Try load PFX from Redis
        RedisValue stored = await db.StringGetAsync(redisKey);
        if (stored.HasValue)
        {
            byte[]? pfxBytes = (byte[])stored; // Redis supports raw binary values
                                               // Load ephemerally (no OS key store writes)
            return new X509Certificate2(
                pfxBytes, pfxPassword,
                X509KeyStorageFlags.EphemeralKeySet | X509KeyStorageFlags.Exportable);
        }

        // 2) Create self-signed cert
        using RSA rsa = RSA.Create(keySize);
        X500DistinguishedName dn = new($"CN={dnsName}");

        CertificateRequest req = new(
            dn, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        // Key usage, SKI, SAN (modern clients expect SAN)
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

        // Export to PFX and save in Redis (optionally set TTL ~= cert lifetime)
        byte[] pfx = created.Export(X509ContentType.Pfx, pfxPassword);
        _ = await db.StringSetAsync(redisKey, pfx);
        if (ttl.HasValue)
        {
            _ = await db.KeyExpireAsync(redisKey, ttl);
        }

        // Return an ephemeral instance to avoid persisting keys on disk
        return new X509Certificate2(
                pfx, pfxPassword,
                X509KeyStorageFlags.EphemeralKeySet | X509KeyStorageFlags.Exportable);
    }

    public static X509Certificate2 GenerateCertificate(string domain, string password)
    {
        string pfxPath = Path.Combine(Environment.CurrentDirectory, "DataProtection.pfx");
        X509Certificate2? certificate = null;
        if (File.Exists(pfxPath))
        {
            byte[] bytes = File.ReadAllBytes(pfxPath);
            certificate = new X509Certificate2(bytes, password, X509KeyStorageFlags.MachineKeySet);
        }
        else
        {
            X500DistinguishedName distinguishedName = new($"CN={domain}");
            using RSA rsa = RSA.Create(2048);
            CertificateRequest request = new(distinguishedName, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, critical: true));
            request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
            request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));
            certificate = request.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(1));
            byte[] pfxData = certificate.Export(X509ContentType.Pfx, password);
            File.WriteAllBytes(pfxPath, pfxData);
        }
        return certificate;
    }
}
