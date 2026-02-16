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
    IOptionsMonitor<MinioOptions> minioOptions) : IObjectStorageService
{
    private MinioOptions options => minioOptions.CurrentValue;
    public async Task EnsureBucketAsync(CancellationToken cancellationToken = default)
    {
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
        }
    }

    public async Task UploadAsync(
        string objectKey,
        string contentType,
        Stream data,
        long size,
        CancellationToken cancellationToken = default)
    {
        if (data.CanSeek)
        {
            _ = data.Seek(0, SeekOrigin.Begin);
        }

        PutObjectArgs args = new PutObjectArgs()
            .WithBucket(options.BucketName)
            .WithObject(objectKey)
            .WithStreamData(data)
            .WithObjectSize(size)
            .WithContentType(contentType);

        _ = await client.PutObjectAsync(args, cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> ExistsAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        try
        {
            _ = await GetMetadataAsync(objectKey, cancellationToken).ConfigureAwait(false);
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
    public async Task StreamToAsync
        (
            string objectKey,
            Stream destination,
            long? offset = null,
            long? length = null,
            CancellationToken cancellationToken = default
        )
    {
        GetObjectArgs args = new GetObjectArgs()
            .WithBucket(options.BucketName)
            .WithObject(objectKey)
            .WithCallbackStream(async (stream, innerCancellationToken) =>
            {
                await stream.CopyToAsync(destination, innerCancellationToken);
            });
        if (offset is not null && length is not null)
        {
            long resolvedOffset = Math.Max(0, offset.Value);
            long resolvedLength = Math.Max(0, length.Value);

            var statArgs = new StatObjectArgs()
                .WithBucket(options.BucketName)
                .WithObject(objectKey);
            var stat = await client.StatObjectAsync(statArgs, cancellationToken).ConfigureAwait(false);
            long availableLength = stat.Size - resolvedOffset;
            if (availableLength < 0)
            {
                return;
            }
            resolvedLength = Math.Min(resolvedLength, availableLength);

            args = args.WithOffsetAndLength(resolvedOffset, resolvedLength);
        }

        await client.GetObjectAsync(args, cancellationToken).ConfigureAwait(false);
    }

    public async Task RemoveAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        RemoveObjectArgs args = new RemoveObjectArgs()
            .WithBucket(options.BucketName)
            .WithObject(objectKey);

        await client.RemoveObjectAsync(args, cancellationToken).ConfigureAwait(false);
    }

    private async Task<ObjectStat> GetStatAsync(string objectKey, CancellationToken cancellationToken)
    {
        StatObjectArgs args = new StatObjectArgs()
            .WithBucket(options.BucketName)
            .WithObject(objectKey);
        return await client.StatObjectAsync(args, cancellationToken).ConfigureAwait(false);
    }
}
