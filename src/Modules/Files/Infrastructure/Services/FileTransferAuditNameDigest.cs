using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Vitarish.Files.Infrastructure.Options;

namespace Vitarish.Files.Infrastructure.Services;

/// <summary>Computes the keyed digest of a normalized file name kept in file transfer audit records.</summary>
public sealed class FileTransferAuditNameDigest
{
    private readonly byte[] key;

    public FileTransferAuditNameDigest(IOptions<FileTransferAuditKeyOptions> options, IHostEnvironment? environment = null)
    {
        string resolved = FileTransferAuditKeyOptionsValidator.ResolveKey(options.Value, environment)
            ?? throw new InvalidOperationException("The file transfer audit key is not configured.");
        key = Encoding.UTF8.GetBytes(resolved);
    }

    public string Compute(string normalizedFileName) =>
        Convert.ToHexStringLower(HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(normalizedFileName)));
}
