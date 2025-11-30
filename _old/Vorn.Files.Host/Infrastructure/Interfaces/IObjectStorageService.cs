using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Vorn.Files.Host.Infrastructure.Interfaces;

public interface IObjectStorageService
{
    Task EnsureBucketAsync(CancellationToken cancellationToken = default);
    Task UploadAsync(string objectKey, string contentType, Stream data, long size, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string objectKey, CancellationToken cancellationToken = default);
    Task<ObjectMetadata> GetMetadataAsync(string objectKey, CancellationToken cancellationToken = default);
    Task<Stream> DownloadAsync(string objectKey, CancellationToken cancellationToken = default);
    Task StreamToAsync(
        string objectKey,
        Stream destination,
        long? offset = null,
        long? length = null,
        CancellationToken cancellationToken = default);
    Task RemoveAsync(string objectKey, CancellationToken cancellationToken = default);
}

public sealed record ObjectMetadata(long Size, string? ContentType);
