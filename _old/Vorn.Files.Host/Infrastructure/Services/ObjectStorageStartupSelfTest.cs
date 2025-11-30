
using Microsoft.Extensions.Logging.Abstractions;
using Minio;
using System.Buffers;
using System.Net.Http;
using Vorn.Files.Host.Domain.Options;
using Vorn.Files.Host.Infrastructure.Extensions;
using Vorn.Files.Host.Infrastructure.Interfaces;

namespace Vorn.Files.Host.Infrastructure.Services;

public sealed class ObjectStorageStartupSelfTest(
    IObjectStorageService storage,
    VornOptions options,
    MinioOptions minioOptions,
    IMinioClient minioClient,
    IHttpClientFactory httpClientFactory,
    ILogger<ObjectStorageStartupSelfTest> logger) : IHostedService
{
    private const string SelfTestFileName = "_startup-self-test.bin";
    private readonly FilesOptions filesOptions = options.Files
        ?? throw new InvalidOperationException("Files options are required for the startup self-test.");
    private static readonly byte[] Payload = "Vorn.Files startup self-test"u8.ToArray();

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        string objectKey = GetObjectKey();
        try
        {
            await storage.EnsureBucketAsync(cancellationToken).ConfigureAwait(false);

            await UploadAsync(cancellationToken).ConfigureAwait(false);
            await VerifyExistsAsync(cancellationToken).ConfigureAwait(false);
            await VerifyDownloadAsync(cancellationToken).ConfigureAwait(false);
            await VerifySeekAsync(cancellationToken).ConfigureAwait(false);
            await VerifyMiddlewareAsync(cancellationToken).ConfigureAwait(false);
            await VerifyRemoveAsync(cancellationToken).ConfigureAwait(false);

            logger.LogInformation("Object storage startup self-test completed successfully.");
        }
        catch(OperationCanceledException)
        {
            throw;
        }
        catch(System.Exception ex)
        {
            logger.LogCritical(ex, "Object storage startup self-test failed.");
            throw;
        }
        finally
        {
            await CleanupAsync(objectKey, cancellationToken).ConfigureAwait(false);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private async Task UploadAsync(CancellationToken cancellationToken)
    {
        await using MemoryStream buffer = new(Payload, writable: false);
        await storage.UploadAsync(
            GetObjectKey(),
            "application/octet-stream",
            buffer,
            Payload.Length,
            cancellationToken).ConfigureAwait(false);
    }

    private async Task VerifyExistsAsync(CancellationToken cancellationToken)
    {
        string objectKey = GetObjectKey();
        bool exists = await storage.ExistsAsync(objectKey, cancellationToken).ConfigureAwait(false);
        if(!exists)
        {
            throw new IOException($"Uploaded self-test object '{objectKey}' was not found.");
        }

        ObjectMetadata metadata = await storage.GetMetadataAsync(objectKey, cancellationToken)
            .ConfigureAwait(false);
        if(metadata.Size != Payload.LongLength)
        {
            throw new IOException(
                $"Self-test object '{objectKey}' reported unexpected size {metadata.Size}, expected {Payload.Length}.");
        }
    }

    private async Task VerifyDownloadAsync(CancellationToken cancellationToken)
    {
        await using Stream download = await storage.DownloadAsync(GetObjectKey(), cancellationToken).ConfigureAwait(false);
        byte[] buffer = ArrayPool<byte>.Shared.Rent(Payload.Length);
        try
        {
            int read = await download.ReadAsync(buffer.AsMemory(0, Payload.Length), cancellationToken).ConfigureAwait(false);
            if(read != Payload.Length)
            {
                throw new IOException(
                    $"Self-test download returned {read} bytes, expected {Payload.Length}.");
            }

            if(!Payload.AsSpan().SequenceEqual(buffer.AsSpan(0, Payload.Length)))
            {
                throw new IOException("Self-test download returned unexpected payload.");
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private async Task VerifySeekAsync(CancellationToken cancellationToken)
    {
        int sliceOffset = 5;
        int sliceLength = 7;
        await using MemoryStream sliceBuffer = new();
        await storage.StreamToAsync(
            GetObjectKey(),
            sliceBuffer,
            sliceOffset,
            sliceLength,
            cancellationToken).ConfigureAwait(false);

        byte[] slice = sliceBuffer.ToArray();
        if(slice.Length != sliceLength)
        {
            throw new IOException(
                $"Self-test seek returned {slice.Length} bytes, expected {sliceLength}.");
        }

        if(!Payload.AsSpan(sliceOffset, sliceLength).SequenceEqual(slice))
        {
            throw new IOException("Self-test seek returned unexpected payload slice.");
        }
    }

    private async Task VerifyMiddlewareAsync(CancellationToken cancellationToken)
    {
        DefaultHttpContext context = new()
        {
            RequestAborted = cancellationToken,
        };

        string requestPath = BuildRequestPath();
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = requestPath;
        context.Request.Headers.Range = "bytes=5-11";
        context.Items["FileName"] = SelfTestFileName;

        await using MemoryStream responseStream = new();
        context.Response.Body = responseStream;

        ObjectStorageFileMiddleware middleware = new(
            _ => Task.CompletedTask,
            options,
            minioOptions,
            minioClient,
            httpClientFactory,
            NullLogger<ObjectStorageFileMiddleware>.Instance);

        await middleware.InvokeAsync(context).ConfigureAwait(false);

        if (context.Response.StatusCode != StatusCodes.Status206PartialContent)
        {
            throw new IOException("Object storage middleware did not return a partial content response during self-test.");
        }

        if (context.Response.Headers.AcceptRanges != "bytes")
        {
            throw new IOException("Object storage middleware did not advertise range support during self-test.");
        }

        if (context.Response.ContentLength is not long length || length != 7)
        {
            throw new IOException($"Object storage middleware returned unexpected content length {context.Response.ContentLength} during self-test.");
        }

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        byte[] slice = responseStream.ToArray();
        if (!Payload.AsSpan(5, 7).SequenceEqual(slice))
        {
            throw new IOException("Object storage middleware returned unexpected payload during self-test.");
        }
    }

    private async Task VerifyRemoveAsync(CancellationToken cancellationToken)
    {
        string objectKey = GetObjectKey();
        await storage.RemoveAsync(objectKey, cancellationToken).ConfigureAwait(false);
        bool exists = await storage.ExistsAsync(objectKey, cancellationToken).ConfigureAwait(false);
        if(exists)
        {
            throw new IOException($"Self-test object '{objectKey}' still exists after removal.");
        }
    }

    private async Task CleanupAsync(string objectKey, CancellationToken cancellationToken)
    {
        try
        {
            if(await storage.ExistsAsync(objectKey, cancellationToken).ConfigureAwait(false))
            {
                await storage.RemoveAsync(objectKey, cancellationToken).ConfigureAwait(false);
            }
        }
        catch(OperationCanceledException)
        {
            throw;
        }
        catch(System.Exception ex)
        {
            logger.LogWarning(ex, "Failed to clean up self-test object {ObjectKey}.", objectKey);
        }
    }

    private string BuildRequestPath()
    {
        string uploadPath = filesOptions.UploadPath?.Trim('/') ?? string.Empty;
        return string.IsNullOrEmpty(uploadPath)
            ? $"/{SelfTestFileName}"
            : $"/{uploadPath}/{SelfTestFileName}";
    }

    private string GetObjectKey() => filesOptions.BuildObjectKey(SelfTestFileName);
}
