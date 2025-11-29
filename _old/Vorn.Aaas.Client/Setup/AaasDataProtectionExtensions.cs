using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
public static class AaasDataProtectionExtensions
{
    public static void AddAaasDataProtection(this WebApplicationBuilder builder)
    {
        string domain = builder.Configuration["Vorn:Aaas:Authority"] ?? throw new Exception("Vorn:Aaas:Authority not set");
        string pfxPassword = builder.Configuration["Vorn:Aaas:DataProtectionPassword"] ?? throw new Exception("Vorn:Aaas:DataProtectionPassword not set");
        X509Certificate2 cert = GenerateCertificate(domain, pfxPassword);
        builder.Services
        .AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo("Keys"))
            .ProtectKeysWithCertificate(cert)
            .SetApplicationName(domain);
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
