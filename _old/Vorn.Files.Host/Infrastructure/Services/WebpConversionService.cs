namespace Vorn.Files.Host.Infrastructure.Services;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SkiaSharp;
using System;
using System.IO;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Vorn.Files.Host.Infrastructure.Interfaces;

public sealed class WebpConversionService(
    ILogger<WebpConversionService> logger,
    IObjectStorageService storage) : BackgroundService, IWebpConversionService
{
    private readonly Channel<ConversionRequest> channel = Channel.CreateUnbounded<ConversionRequest>();

    public void Enqueue(string objectKey, string contentType)
        => channel.Writer.TryWrite(new(objectKey, contentType));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (ConversionRequest request in channel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                if (!request.ContentType.StartsWith("image", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string webpObjectKey = Path.ChangeExtension(request.ObjectKey, "webp")!;
                if (await storage.ExistsAsync(webpObjectKey, stoppingToken).ConfigureAwait(false))
                {
                    continue;
                }

                await using Stream stream = await storage.DownloadAsync(request.ObjectKey, stoppingToken).ConfigureAwait(false);
                using SKBitmap bitmap = SKBitmap.Decode(stream);
                if (bitmap is null)
                {
                    logger.LogWarning("Skipping WEBP conversion for {File}: unable to decode bitmap", request.ObjectKey);
                    continue;
                }

                using SKImage image = SKImage.FromBitmap(bitmap);
                using SKData? data = image.Encode(SKEncodedImageFormat.Webp, 80);
                if (data is null)
                {
                    logger.LogWarning("Skipping WEBP conversion for {File}: encoder returned null data", request.ObjectKey);
                    continue;
                }

                using MemoryStream destination = new();
                data.SaveTo(destination);
                destination.Seek(0, SeekOrigin.Begin);

                await storage.UploadAsync(
                    webpObjectKey,
                    "image/webp",
                    destination,
                    destination.Length,
                    stoppingToken).ConfigureAwait(false);

                logger.LogInformation("Generated WEBP variant for {File}", request.ObjectKey);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "WEBP conversion failed for {File}", request.ObjectKey);
            }
        }
    }

    private readonly record struct ConversionRequest(string ObjectKey, string ContentType);
}
