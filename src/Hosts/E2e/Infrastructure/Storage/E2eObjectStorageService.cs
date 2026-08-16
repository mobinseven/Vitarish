using System.Collections.Concurrent;
using Vitarish.Files.Application.Abstractions;

namespace Vitarish.E2e.Infrastructure.Storage;

internal sealed class E2eObjectStorageService : IObjectStorageService
{
    private readonly ConcurrentDictionary<string, StoredObject> objects = new(StringComparer.Ordinal);

    public Task EnsureBucketAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public async Task UploadAsync(string objectKey, string contentType, Stream data, long size, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(objectKey);
        await using MemoryStream buffer = new();
        await data.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
        objects[objectKey] = new(buffer.ToArray(), contentType);
    }

    public Task<bool> ExistsAsync(string objectKey, CancellationToken cancellationToken = default) =>
        Task.FromResult(objects.ContainsKey(objectKey));

    public Task<ObjectMetadata> GetMetadataAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        StoredObject stored = Get(objectKey);
        return Task.FromResult(new ObjectMetadata(stored.Content.Length, stored.ContentType));
    }

    public Task<Stream> DownloadAsync(string objectKey, CancellationToken cancellationToken = default) =>
        Task.FromResult<Stream>(new MemoryStream(Get(objectKey).Content, writable: false));

    public async Task StreamToAsync(
        string objectKey,
        Stream destination,
        long? offset = null,
        long? length = null,
        CancellationToken cancellationToken = default)
    {
        StoredObject stored = Get(objectKey);
        long start = Math.Clamp(offset ?? 0, 0, stored.Content.LongLength);
        long available = stored.Content.LongLength - start;
        long count = Math.Clamp(length ?? available, 0, available);
        await destination.WriteAsync(stored.Content.AsMemory((int)start, (int)count), cancellationToken).ConfigureAwait(false);
    }

    public Task RemoveAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        _ = objects.TryRemove(objectKey, out _);
        return Task.CompletedTask;
    }

    public Task RenameAsync(string objectKey, string newObjectKey, CancellationToken cancellationToken = default)
    {
        StoredObject stored = Get(objectKey);
        objects[newObjectKey] = stored;
        _ = objects.TryRemove(objectKey, out _);
        return Task.CompletedTask;
    }

    public Task<ObjectListResult> ListAsync(
        string? prefix = null,
        string? continuationToken = null,
        int? maxKeys = null,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<KeyValuePair<string, StoredObject>> matches = objects
            .Where(item => string.IsNullOrWhiteSpace(prefix) || item.Key.StartsWith(prefix, StringComparison.Ordinal));
        if (maxKeys is > 0)
            matches = matches.Take(maxKeys.Value);

        ObjectListItem[] items = matches
            .Select(item => new ObjectListItem(item.Key, item.Value.Content.LongLength, DateTimeOffset.UtcNow, null))
            .ToArray();
        return Task.FromResult(new ObjectListResult(items, null, false));
    }

    public Task<string> GetPresignedUrlAsync(
        string objectKey,
        TimeSpan expiresIn,
        string httpMethod = "GET",
        CancellationToken cancellationToken = default) =>
        Task.FromResult($"e2e://object-storage/{Uri.EscapeDataString(objectKey)}");

    private StoredObject Get(string objectKey) =>
        objects.TryGetValue(objectKey, out StoredObject? stored)
            ? stored
            : throw new FileNotFoundException("The E2E object was not found.", objectKey);

    private sealed record StoredObject(byte[] Content, string? ContentType);
}
