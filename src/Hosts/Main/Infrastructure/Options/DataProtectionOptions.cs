namespace Vitarish.Main.Infrastructure.Options;

public sealed class DataProtectionOptions
{
    public string KeyRingRedisKey { get; init; } = string.Empty;
    public string CertificateRedisKey { get; init; } = string.Empty;
    public string[] PreviousCertificateRedisKeys { get; init; } = [];
}
