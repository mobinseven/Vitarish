namespace Vitarish.Files.Application.Abstractions;

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
    Task RenameAsync(string objectKey, string newObjectKey, CancellationToken cancellationToken = default);
    Task<ObjectListResult> ListAsync(
        string? prefix = null,
        string? continuationToken = null,
        int? maxKeys = null,
        CancellationToken cancellationToken = default);
    Task<string> GetPresignedUrlAsync(
        string objectKey,
        TimeSpan expiresIn,
        string httpMethod = "GET",
        CancellationToken cancellationToken = default);
}

public sealed record ObjectMetadata(long Size, string? ContentType);
public sealed record ObjectListItem(string Key, long Size, DateTimeOffset LastModified, string? ETag);
public sealed record ObjectListResult(IReadOnlyList<ObjectListItem> Items, string? ContinuationToken, bool IsTruncated);
