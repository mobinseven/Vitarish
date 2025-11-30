using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Vitarish.Web.Clients.Setup;

/// <summary>
/// Extension methods for configuring data protection.
/// </summary>
public static class IdentityDataProtectionExtensions
{
    /// <summary>
    /// Adds identity-related data protection configuration.
    /// </summary>
    public static void AddIdentityDataProtection(this WebApplicationBuilder builder)
    {
        string domain = builder.Configuration["Vitarish:Identity:Authority"]
            ?? throw new InvalidOperationException("Vitarish:Identity:Authority not set");

        string pfxPassword = builder.Configuration["Vitarish:Identity:DataProtectionPassword"]
            ?? throw new InvalidOperationException("Vitarish:Identity:DataProtectionPassword not set");

        X509Certificate2 cert = GenerateCertificate(domain, pfxPassword);

        _ = builder.Services
            .AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo("Keys"))
            .ProtectKeysWithCertificate(cert)
            .SetApplicationName(domain);
    }

    /// <summary>
    /// Generates or loads a self-signed certificate for data protection.
    /// </summary>
    public static X509Certificate2 GenerateCertificate(string domain, string password)
    {
        string pfxPath = Path.Combine(Environment.CurrentDirectory, "DataProtection.pfx");
        X509Certificate2? certificate;

        if (File.Exists(pfxPath))
        {
            byte[] bytes = File.ReadAllBytes(pfxPath);
#pragma warning disable SYSLIB0057 // X509CertificateLoader not available on all platforms
            certificate = new X509Certificate2(bytes, password, X509KeyStorageFlags.MachineKeySet);
#pragma warning restore SYSLIB0057
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
