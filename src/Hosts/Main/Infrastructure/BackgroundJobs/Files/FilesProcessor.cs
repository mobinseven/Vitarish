using Microsoft.Extensions.Logging.Abstractions;
using Minio;
using System.Buffers;
using Vitarish.Files.Application.Abstractions;
using Vitarish.Files.Domain.Options;
using Vitarish.Files.Infrastructure.Extensions;
using Vitarish.Files.Infrastructure.Interfaces;
using Vitarish.Files.Infrastructure.Services;
namespace Vitarish.Main.Infrastructure.BackgroundJobs.Files;

public class FilesProcessor(
    IFilesModule filesModule,
    IObjectStorageService storage,
    IOptionsMonitor<FilesOptions> filesOptions,
    IOptionsMonitor<MinioOptions> minioOptions,
    IMinioClient minioClient,
    IHttpClientFactory httpClientFactory)
{

    private const string SelfTestFileName = "_startup-self-test.bin";
    private static readonly byte[] Payload = "Files module startup self-test"u8.ToArray();

    public async Task ProcessSelfTests(CancellationToken cancellationToken = default)
    {
        string objectKey = GetObjectKey(filesOptions.CurrentValue);
        try
        {
            await storage.EnsureBucketAsync(cancellationToken).ConfigureAwait(false);

            await UploadAsync(filesOptions.CurrentValue, cancellationToken).ConfigureAwait(false);
            await VerifyExistsAsync(filesOptions.CurrentValue, cancellationToken).ConfigureAwait(false);
            await VerifyDownloadAsync(filesOptions.CurrentValue, cancellationToken).ConfigureAwait(false);
            await VerifySeekAsync(filesOptions.CurrentValue, cancellationToken).ConfigureAwait(false);
            await VerifyMiddlewareAsync(filesOptions.CurrentValue, minioOptions.CurrentValue, cancellationToken).ConfigureAwait(false);
            await VerifyRemoveAsync(filesOptions.CurrentValue, cancellationToken).ConfigureAwait(false);

            filesModule.IsAvailable = true;
        }
        catch
        {
            filesModule.IsAvailable = false;
        }
        finally
        {
            await CleanupAsync(objectKey, filesOptions.CurrentValue, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task UploadAsync(FilesOptions filesOptions, CancellationToken cancellationToken)
    {
        await using MemoryStream buffer = new(Payload, writable: false);
        await storage.UploadAsync(
            GetObjectKey(filesOptions),
            "application/octet-stream",
            buffer,
            Payload.Length,
            cancellationToken).ConfigureAwait(false);
    }

    private async Task VerifyExistsAsync(FilesOptions filesOptions, CancellationToken cancellationToken)
    {
        string objectKey = GetObjectKey(filesOptions);
        bool exists = await storage.ExistsAsync(objectKey, cancellationToken).ConfigureAwait(false);
        if (!exists)
        {
            throw new IOException($"Uploaded self-test object '{objectKey}' was not found.");
        }

        ObjectMetadata metadata = await storage.GetMetadataAsync(objectKey, cancellationToken)
            .ConfigureAwait(false);
        if (metadata.Size != Payload.LongLength)
        {
            throw new IOException(
                $"Self-test object '{objectKey}' reported unexpected size {metadata.Size}, expected {Payload.Length}.");
        }
    }

    private async Task VerifyDownloadAsync(FilesOptions filesOptions, CancellationToken cancellationToken)
    {
        await using Stream download = await storage.DownloadAsync(GetObjectKey(filesOptions), cancellationToken).ConfigureAwait(false);
        byte[] buffer = ArrayPool<byte>.Shared.Rent(Payload.Length);
        try
        {
            int read = await download.ReadAsync(buffer.AsMemory(0, Payload.Length), cancellationToken).ConfigureAwait(false);
            if (read != Payload.Length)
            {
                throw new IOException(
                    $"Self-test download returned {read} bytes, expected {Payload.Length}.");
            }

            if (!Payload.AsSpan().SequenceEqual(buffer.AsSpan(0, Payload.Length)))
            {
                throw new IOException("Self-test download returned unexpected payload.");
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private async Task VerifySeekAsync(FilesOptions filesOptions, CancellationToken cancellationToken)
    {
        int sliceOffset = 5;
        int sliceLength = 7;
        await using MemoryStream sliceBuffer = new();
        await storage.StreamToAsync(
            GetObjectKey(filesOptions),
            sliceBuffer,
            sliceOffset,
            sliceLength,
            cancellationToken).ConfigureAwait(false);

        byte[] slice = sliceBuffer.ToArray();
        if (slice.Length != sliceLength)
        {
            throw new IOException(
                $"Self-test seek returned {slice.Length} bytes, expected {sliceLength}.");
        }

        if (!Payload.AsSpan(sliceOffset, sliceLength).SequenceEqual(slice))
        {
            throw new IOException("Self-test seek returned unexpected payload slice.");
        }
    }
    private async Task VerifyRemoveAsync(FilesOptions filesOptions, CancellationToken cancellationToken)
    {
        string objectKey = GetObjectKey(filesOptions);
        await storage.RemoveAsync(objectKey, cancellationToken).ConfigureAwait(false);
        bool exists = await storage.ExistsAsync(objectKey, cancellationToken).ConfigureAwait(false);
        if (exists)
        {
            throw new IOException($"Self-test object '{objectKey}' still exists after removal.");
        }
    }

    private async Task CleanupAsync(string objectKey, FilesOptions filesOptions, CancellationToken cancellationToken)
    {
        try
        {
            if (await storage.ExistsAsync(objectKey, cancellationToken).ConfigureAwait(false))
            {
                await storage.RemoveAsync(objectKey, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (System.Exception)
        {
            throw;
        }
    }

    private async Task VerifyMiddlewareAsync(FilesOptions filesOptions, MinioOptions minioOptions, CancellationToken cancellationToken)
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

        ServiceCollection services = new();
        _ = services.AddSingleton<IOptionsSnapshot<FilesOptions>>(new TestOptionsSnapshot<FilesOptions>(filesOptions));
        _ = services.AddSingleton<IOptionsSnapshot<MinioOptions>>(new TestOptionsSnapshot<MinioOptions>(minioOptions));
        await using ServiceProvider provider = services.BuildServiceProvider();
        context.RequestServices = provider;

        await using MemoryStream responseStream = new();
        context.Response.Body = responseStream;

        ObjectStorageMiddleware middleware = new(
            _ => Task.CompletedTask,
            minioClient,
            httpClientFactory,
            NullLogger<ObjectStorageMiddleware>.Instance);

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

        _ = context.Response.Body.Seek(0, SeekOrigin.Begin);
        byte[] slice = responseStream.ToArray();
        if (!Payload.AsSpan(5, 7).SequenceEqual(slice))
        {
            throw new IOException("Object storage middleware returned unexpected payload during self-test.");
        }
    }
    private string BuildRequestPath()
    {
        string uploadPath = filesOptions.CurrentValue?.UploadPath?.Trim('/') ?? string.Empty;
        return string.IsNullOrEmpty(uploadPath)
            ? $"/{SelfTestFileName}"
            : $"/{uploadPath}/{SelfTestFileName}";
    }

    private string GetObjectKey(FilesOptions filesOptions) => filesOptions.BuildObjectKey(SelfTestFileName);
}

internal sealed class TestOptionsSnapshot<TOptions> : IOptionsSnapshot<TOptions>
    where TOptions : class, new()
{
    public TestOptionsSnapshot(TOptions value)
    {
        Value = value;
    }

    public TOptions Value { get; }

    public TOptions Get(string? name) => Value;
}
