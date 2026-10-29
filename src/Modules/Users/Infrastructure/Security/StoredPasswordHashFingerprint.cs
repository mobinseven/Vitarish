using System.Security.Cryptography;
using System.Text;

namespace Vitarish.Users.Infrastructure.Security;

internal static class StoredPasswordHashFingerprint
{
    internal static byte[] Create(string passwordHash)
    {
        ArgumentException.ThrowIfNullOrEmpty(passwordHash);
        byte[] encodedPasswordHash = Encoding.UTF8.GetBytes(passwordHash);
        try
        {
            return SHA256.HashData(encodedPasswordHash);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(encodedPasswordHash);
        }
    }

    internal static bool Matches(string? passwordHash, byte[]? fingerprint)
    {
        if (string.IsNullOrEmpty(passwordHash) || fingerprint is null || fingerprint.Length != SHA256.HashSizeInBytes)
            return false;

        byte[] computedFingerprint = Create(passwordHash);
        try
        {
            return CryptographicOperations.FixedTimeEquals(computedFingerprint, fingerprint);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(computedFingerprint);
        }
    }
}
