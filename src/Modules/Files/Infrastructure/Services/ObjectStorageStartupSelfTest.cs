using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Minio;
using System.Buffers;
using Vitarish.Files.Domain.Options;
using Vitarish.Files.Infrastructure.Extensions;
using Vitarish.Files.Infrastructure.Interfaces;

namespace Vitarish.Files.Infrastructure.Services;

public sealed class ObjectStorageStartupSelfTest(
    IObjectStorageService storage,
    IOptionsMonitor<FilesOptions> options,
    IOptionsMonitor<MinioOptions> minioOptions,
    IMinioClient minioClient,
    IHttpClientFactory httpClientFactory,
    ILogger<ObjectStorageStartupSelfTest> logger) : IHostedService
{
    private readonly IObjectStorageService _storage = storage;
    private readonly IOptionsMonitor<FilesOptions> _filesOptions = options;
    private readonly IOptionsMonitor<MinioOptions> _minioOptions = minioOptions;
    private readonly IMinioClient _minioClient = minioClient;
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly ILogger<ObjectStorageStartupSelfTest> _logger = logger;

    private const string SelfTestFileName = "_startup-self-test.bin";
    private static readonly byte[] Payload = "Files module startup self-test"u8.ToArray();

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        FilesOptions filesOptions = _filesOptions.CurrentValue
            ?? throw new InvalidOperationException("Files options are required for the startup self-test.");
        MinioOptions minioOptions = _minioOptions.CurrentValue;
        string objectKey = GetObjectKey(filesOptions);
        try
        {
            //await _storage.EnsureBucketAsync(cancellationToken).ConfigureAwait(false);

            //await UploadAsync(filesOptions, cancellationToken).ConfigureAwait(false);
            //await VerifyExistsAsync(filesOptions, cancellationToken).ConfigureAwait(false);
            //await VerifyDownloadAsync(filesOptions, cancellationToken).ConfigureAwait(false);
            //await VerifySeekAsync(filesOptions, cancellationToken).ConfigureAwait(false);
            //await VerifyMiddlewareAsync(filesOptions, minioOptions, cancellationToken).ConfigureAwait(false);
            //await VerifyRemoveAsync(filesOptions, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Object storage startup self-test completed successfully.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (System.Exception ex)
        {
            _logger.LogCritical(ex, "Object storage startup self-test failed.");
            throw;
        }
        finally
        {
            await CleanupAsync(objectKey, filesOptions, cancellationToken).ConfigureAwait(false);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task UploadAsync(FilesOptions filesOptions, CancellationToken cancellationToken)
    {
        await using MemoryStream buffer = new(Payload, writable: false);
        await _storage.UploadAsync(
            GetObjectKey(filesOptions),
            "application/octet-stream",
            buffer,
            Payload.Length,
            cancellationToken).ConfigureAwait(false);
    }

    private async Task VerifyExistsAsync(FilesOptions filesOptions, CancellationToken cancellationToken)
    {
        string objectKey = GetObjectKey(filesOptions);
        bool exists = await _storage.ExistsAsync(objectKey, cancellationToken).ConfigureAwait(false);
        if (!exists)
        {
            throw new IOException($"Uploaded self-test object '{objectKey}' was not found.");
        }

        ObjectMetadata metadata = await _storage.GetMetadataAsync(objectKey, cancellationToken)
            .ConfigureAwait(false);
        if (metadata.Size != Payload.LongLength)
        {
            throw new IOException(
                $"Self-test object '{objectKey}' reported unexpected size {metadata.Size}, expected {Payload.Length}.");
        }
    }

    private async Task VerifyDownloadAsync(FilesOptions filesOptions, CancellationToken cancellationToken)
    {
        await using Stream download = await _storage.DownloadAsync(GetObjectKey(filesOptions), cancellationToken).ConfigureAwait(false);
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
        await _storage.StreamToAsync(
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
            _minioClient,
            _httpClientFactory,
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

    private async Task VerifyRemoveAsync(FilesOptions filesOptions, CancellationToken cancellationToken)
    {
        string objectKey = GetObjectKey(filesOptions);
        await _storage.RemoveAsync(objectKey, cancellationToken).ConfigureAwait(false);
        bool exists = await _storage.ExistsAsync(objectKey, cancellationToken).ConfigureAwait(false);
        if (exists)
        {
            throw new IOException($"Self-test object '{objectKey}' still exists after removal.");
        }
    }

    private async Task CleanupAsync(string objectKey, FilesOptions filesOptions, CancellationToken cancellationToken)
    {
        try
        {
            if (await _storage.ExistsAsync(objectKey, cancellationToken).ConfigureAwait(false))
            {
                await _storage.RemoveAsync(objectKey, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (System.Exception ex)
        {
            _logger.LogWarning(ex, "Failed to clean up self-test object {ObjectKey}.", objectKey);
        }
    }

    private string BuildRequestPath()
    {
        string uploadPath = _filesOptions.CurrentValue?.UploadPath?.Trim('/') ?? string.Empty;
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
