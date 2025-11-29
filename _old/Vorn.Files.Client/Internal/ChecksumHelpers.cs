using System.Security.Cryptography;

namespace Vorn.Files.Client.Internal
{
    internal static class ChecksumHelpers
    {
        public static async Task<string> ComputeSHA256Checksum(Stream stream, CancellationToken cancellationToken = default)
        {
            // logging not available here since static, just compute
            using SHA256 sha256 = SHA256.Create();
            byte[] hashBytes = await sha256.ComputeHashAsync(stream, cancellationToken);
            stream.Position = 0;
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }
    }
}