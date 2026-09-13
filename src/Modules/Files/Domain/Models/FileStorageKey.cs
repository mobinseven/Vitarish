using System.Security.Cryptography;
using System.Text;

namespace Vitarish.Files.Domain.Models;

/// <summary>
/// Produces a tenant-scoped object identity from trusted persisted values.
/// </summary>
public static class FileStorageKey
{
    public static string For(string tenantId, Guid fileId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentOutOfRangeException.ThrowIfEqual(fileId, Guid.Empty);

        byte[] tenantHash = SHA256.HashData(Encoding.UTF8.GetBytes(tenantId));
        try
        {
            return $"{Convert.ToHexString(tenantHash).ToLowerInvariant()}/{fileId:N}";
        }
        finally
        {
            CryptographicOperations.ZeroMemory(tenantHash);
        }
    }
}
