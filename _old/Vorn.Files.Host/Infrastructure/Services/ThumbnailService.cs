namespace Vorn.Files.Host.Infrastructure.Services;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Vorn.Files.Host.Infrastructure.Extensions;
using Vorn.Files.Host.Infrastructure.Interfaces;

public sealed class ThumbnailService(
    ILogger<ThumbnailService> logger,
    IObjectStorageService storage) : BackgroundService, IThumbnailService
{
    private readonly Channel<ThumbnailRequest> _channel = Channel.CreateUnbounded<ThumbnailRequest>();

    public void Enqueue(string objectKey, string contentType) => _channel.Writer.TryWrite(new(objectKey, contentType));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (ThumbnailRequest req in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                string thumbObjectKey = req.ObjectKey + ".thumb.jpg";

                if (await storage.ExistsAsync(thumbObjectKey, stoppingToken).ConfigureAwait(false))
                {
                    continue;
                }

                await using Stream source = await storage.DownloadAsync(req.ObjectKey, stoppingToken).ConfigureAwait(false);
                byte[] bytes;
                if (req.ContentType.StartsWith("image", StringComparison.OrdinalIgnoreCase))
                {
                    string base64 = Thumbnails.CreateImageThumbnail(source);
                    bytes = Convert.FromBase64String(base64);
                }
                else if (req.ContentType.StartsWith("video", StringComparison.OrdinalIgnoreCase))
                {
                    string base64 = await Thumbnails.CreateVideoThumbnail(source);
                    bytes = Convert.FromBase64String(base64);
                }
                else
                {
                    continue;
                }

                using MemoryStream thumbnailStream = new(bytes);
                await storage.UploadAsync(
                    thumbObjectKey,
                    Thumbnails.ThumbnailContentType,
                    thumbnailStream,
                    thumbnailStream.Length,
                    stoppingToken).ConfigureAwait(false);

                logger.LogInformation("Generated thumbnail for {File}", req.ObjectKey);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Thumbnail generation failed for {File}", req.ObjectKey);
            }
        }
    }

    private record ThumbnailRequest(string ObjectKey, string ContentType);
}
