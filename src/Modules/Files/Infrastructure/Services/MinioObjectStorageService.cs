using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel;
using Minio.DataModel.Args;
using Minio.Exceptions;
using Vitarish.Files.Domain.Options;
using Vitarish.Files.Infrastructure.Interfaces;

namespace Vitarish.Files.Infrastructure.Services;

public sealed class MinioObjectStorageService(
    IMinioClient client,
    IOptionsMonitor<MinioOptions> minioOptions,
    ILogger<MinioObjectStorageService> logger) : IObjectStorageService
{
    private MinioOptions CurrentMinioOptions => minioOptions.CurrentValue;
    public async Task EnsureBucketAsync(CancellationToken cancellationToken = default)
    {
        MinioOptions options = CurrentMinioOptions;
        bool exists = await client.BucketExistsAsync(new BucketExistsArgs().WithBucket(options.BucketName), cancellationToken)
            .ConfigureAwait(false);

        if (!exists)
        {
            MakeBucketArgs args = new MakeBucketArgs().WithBucket(options.BucketName);
            if (!string.IsNullOrWhiteSpace(options.Region))
            {
                args = args.WithLocation(options.Region);
            }

            await client.MakeBucketAsync(args, cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Created bucket {Bucket}", options.BucketName);
        }
    }

    public async Task UploadAsync(
        string objectKey,
        string contentType,
        Stream data,
        long size,
        CancellationToken cancellationToken = default)
    {
        MinioOptions options = CurrentMinioOptions;
        string key = BuildObjectKey(objectKey);
        if (data.CanSeek)
        {
            _ = data.Seek(0, SeekOrigin.Begin);
        }

        PutObjectArgs args = new PutObjectArgs()
            .WithBucket(options.BucketName)
            .WithObject(key)
            .WithStreamData(data)
            .WithObjectSize(size)
            .WithContentType(contentType);

        _ = await client.PutObjectAsync(args, cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> ExistsAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        try
        {
            _ = await GetStatAsync(objectKey, cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (ObjectNotFoundException)
        {
            return false;
        }
    }

    public async Task<ObjectMetadata> GetMetadataAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        ObjectStat stat = await GetStatAsync(objectKey, cancellationToken).ConfigureAwait(false);
        return new ObjectMetadata(stat.Size, stat.ContentType);
    }

    public async Task<Stream> DownloadAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        MemoryStream buffer = new();
        await StreamToAsync(objectKey, buffer, cancellationToken: cancellationToken).ConfigureAwait(false);
        _ = buffer.Seek(0, SeekOrigin.Begin);
        return buffer;
    }

    public async Task StreamToAsync(
        string objectKey,
        Stream destination,
        long? offset = null,
        long? length = null,
        CancellationToken cancellationToken = default)
    {
        MinioOptions options = CurrentMinioOptions;
        string key = BuildObjectKey(objectKey);
        GetObjectArgs args = new GetObjectArgs()
            .WithBucket(options.BucketName)
            .WithObject(key)
            .WithCallbackStream(async stream =>
            {
                await stream.CopyToAsync(destination, cancellationToken).ConfigureAwait(false);
            });

        if (offset is not null && length is not null)
        {
            long resolvedOffset = Math.Max(0, offset.Value);
            long resolvedLength = Math.Max(0, length.Value);
            args = args.WithOffsetAndLength(resolvedOffset, resolvedLength);
        }

        _ = await client.GetObjectAsync(args, cancellationToken).ConfigureAwait(false);
    }

    public async Task RemoveAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        MinioOptions options = CurrentMinioOptions;
        string key = BuildObjectKey(objectKey);
        RemoveObjectArgs args = new RemoveObjectArgs()
            .WithBucket(options.BucketName)
            .WithObject(key);

        await client.RemoveObjectAsync(args, cancellationToken).ConfigureAwait(false);
    }

    private async Task<ObjectStat> GetStatAsync(string objectKey, CancellationToken cancellationToken)
    {
        MinioOptions options = CurrentMinioOptions;
        string key = BuildObjectKey(objectKey);
        StatObjectArgs args = new StatObjectArgs()
            .WithBucket(options.BucketName)
            .WithObject(key);

        return await client.StatObjectAsync(args, cancellationToken).ConfigureAwait(false);
    }

    private string BuildObjectKey(string objectKey)
    {
        string trimmedKey = objectKey.TrimStart('/');
        string? prefix = CurrentMinioOptions.ObjectPrefix?.Trim('/');
        return string.IsNullOrEmpty(prefix)
            ? trimmedKey
            : string.Join('/', prefix, trimmedKey).Replace("//", "/", StringComparison.Ordinal);
    }
}
