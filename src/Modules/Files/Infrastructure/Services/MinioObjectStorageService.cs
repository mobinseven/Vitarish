using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel;
using Minio.DataModel.Args;
using Minio.Exceptions;
using System.Globalization;
using Vitarish.Files.Domain.Options;
using Vitarish.Files.Infrastructure.Interfaces;

namespace Vitarish.Files.Infrastructure.Services;

public sealed class MinioObjectStorageService(
    IMinioClient client,
    IOptionsMonitor<MinioOptions> minioOptions) : IObjectStorageService
{
    private MinioOptions options => minioOptions.CurrentValue;

    private static async Task<TOut> Perform<TOut>(Func<Task<TOut>> function)
    {

        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        CultureInfo originalUiCulture = CultureInfo.CurrentUICulture;

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

            return await function().ConfigureAwait(false);
        }
        catch(Exception ex)
        {
            throw;
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    private static async Task Perform(Func<Task> function)
    {

        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        CultureInfo originalUiCulture = CultureInfo.CurrentUICulture;

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

            await function().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            throw;
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    public async Task EnsureBucketAsync(CancellationToken cancellationToken = default)
    {
        bool exists = await Perform<bool>(() => client.BucketExistsAsync(new BucketExistsArgs().WithBucket(options.BucketName), cancellationToken));
        if (!exists)
        {
            MakeBucketArgs args = new MakeBucketArgs().WithBucket(options.BucketName);
            if (!string.IsNullOrWhiteSpace(options.Region))
            {
                args = args.WithLocation(options.Region);
            }

            await Perform(() => client.MakeBucketAsync(args, cancellationToken));
        }
    }

    public async Task RenameAsync(
        string objectKey,
        string newObjectKey,
        CancellationToken cancellationToken =default)
    {
        CopyObjectArgs args = new CopyObjectArgs()
            .WithBucket(options.BucketName)
            .WithObject(newObjectKey)
            .WithCopyObjectSource(new CopySourceObjectArgs()
                .WithBucket(options.BucketName)
                .WithObject(objectKey));
        await Perform(() => client.CopyObjectAsync(args, cancellationToken));
        await RemoveAsync(objectKey, cancellationToken);
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

        _ = await Perform(() => client.PutObjectAsync(args, cancellationToken));
    }

    public async Task<bool> ExistsAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        try
        {
            _ = await GetMetadataAsync(objectKey, cancellationToken);
            return true;
        }
        catch (ObjectNotFoundException)
        {
            return false;
        }
    }

    public async Task<ObjectMetadata> GetMetadataAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        ObjectStat stat = await GetStatAsync(objectKey, cancellationToken);
        return new ObjectMetadata(stat.Size, stat.ContentType);
    }

    public async Task<Stream> DownloadAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        MemoryStream buffer = new();
        await StreamToAsync(objectKey, buffer, cancellationToken: cancellationToken);
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
            var stat = await Perform(() => client.StatObjectAsync(statArgs, cancellationToken));
            long availableLength = stat.Size - resolvedOffset;
            if (availableLength < 0)
            {
                return;
            }
            resolvedLength = Math.Min(resolvedLength, availableLength);

            args = args.WithOffsetAndLength(resolvedOffset, resolvedLength);
        }

        await Perform(() => client.GetObjectAsync(args, cancellationToken));
    }

    public async Task RemoveAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        RemoveObjectArgs args = new RemoveObjectArgs()
            .WithBucket(options.BucketName)
            .WithObject(objectKey);

        await Perform(() => client.RemoveObjectAsync(args, cancellationToken));
    }

    private async Task<ObjectStat> GetStatAsync(string objectKey, CancellationToken cancellationToken)
    {
        StatObjectArgs args = new StatObjectArgs()
            .WithBucket(options.BucketName)
            .WithObject(objectKey);
        return await Perform(() => client.StatObjectAsync(args, cancellationToken));
    }
}
