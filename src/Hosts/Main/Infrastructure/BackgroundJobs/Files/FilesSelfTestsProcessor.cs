using System.Buffers;
using Vitarish.Files.Application.Abstractions;
using Vitarish.Files.Domain.Options;
using Vitarish.Files.Infrastructure.Extensions;
using Vitarish.Files.Infrastructure.Interfaces;
namespace Vitarish.Main.Infrastructure.BackgroundJobs.Files;

public class FilesSelfTestsProcessor(
    IFilesModule filesModule,
    IObjectStorageService storage,
    IOptionsMonitor<FilesOptions> filesOptions)
{

    private const string SelfTestFileName = "_startup-self-test.bin";
    private static readonly byte[] Payload = "Files module startup self-test"u8.ToArray();

    public async Task ProcessSelfTests(CancellationToken cancellationToken = default)
    {
        try
        {
            await storage.EnsureBucketAsync(cancellationToken).ConfigureAwait(false);
            await UploadAsync(cancellationToken).ConfigureAwait(false);
            await VerifyExistsAsync(cancellationToken).ConfigureAwait(false);
            await VerifyDownloadAsync(cancellationToken).ConfigureAwait(false);
            await VerifySeekAsync(cancellationToken).ConfigureAwait(false);
            await VerifyRemoveAsync(cancellationToken).ConfigureAwait(false);

            filesModule.IsAvailable = true;
        }
        catch (Exception ex)
        {
            filesModule.IsAvailable = false;
        }
        finally
        {
            await CleanupAsync(cancellationToken).ConfigureAwait(false);
        }
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

    private async Task VerifyDownloadAsync(CancellationToken cancellationToken)
    {
        await using Stream download = await storage.DownloadAsync(GetObjectKey(), cancellationToken).ConfigureAwait(false);
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
    private async Task VerifyRemoveAsync(CancellationToken cancellationToken)
    {
        string objectKey = GetObjectKey();
        await storage.RemoveAsync(objectKey, cancellationToken).ConfigureAwait(false);
        bool exists = await storage.ExistsAsync(objectKey, cancellationToken).ConfigureAwait(false);
        if (exists)
        {
            throw new IOException($"Self-test object '{objectKey}' still exists after removal.");
        }
    }

    private async Task CleanupAsync(CancellationToken cancellationToken)
    {
        string objectKey = GetObjectKey();
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

    private string GetObjectKey() => filesOptions.CurrentValue.BuildObjectKey(SelfTestFileName);
}
