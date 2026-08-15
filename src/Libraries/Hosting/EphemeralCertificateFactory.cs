using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Vitarish.Hosting;

public static class EphemeralCertificateFactory
{
    public static X509Certificate2 Create(string dnsName)
    {
        using RSA rsa = RSA.Create(2048);
        CertificateRequest request = new(new X500DistinguishedName($"CN={dnsName}"), rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, true));
        request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));
        using X509Certificate2 certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddHours(2));
        return X509CertificateLoader.LoadPkcs12(certificate.Export(X509ContentType.Pfx), password: null, X509KeyStorageFlags.EphemeralKeySet | X509KeyStorageFlags.Exportable);
    }
}
