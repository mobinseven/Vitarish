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

public sealed class WebpConversionService(ILogger<WebpConversionService> logger) : BackgroundService, IWebpConversionService
{
    private readonly Channel<ConversionRequest> channel = Channel.CreateUnbounded<ConversionRequest>();

    public void Enqueue(string filePath, string contentType)
        => channel.Writer.TryWrite(new(filePath, contentType));

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

                string webpPath = Path.ChangeExtension(request.FilePath, "webp");
                if (File.Exists(webpPath))
                {
                    continue;
                }

                await using FileStream stream = File.OpenRead(request.FilePath);
                using SKBitmap bitmap = SKBitmap.Decode(stream);
                if (bitmap is null)
                {
                    logger.LogWarning("Skipping WEBP conversion for {File}: unable to decode bitmap", request.FilePath);
                    continue;
                }

                using SKImage image = SKImage.FromBitmap(bitmap);
                using SKData? data = image.Encode(SKEncodedImageFormat.Webp, 80);
                if (data is null)
                {
                    logger.LogWarning("Skipping WEBP conversion for {File}: encoder returned null data", request.FilePath);
                    continue;
                }

                await using FileStream destination = new(
                    webpPath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.Read,
                    bufferSize: 64 * 1024,
                    useAsync: true);

                data.SaveTo(destination);
                await destination.FlushAsync(stoppingToken).ConfigureAwait(false);

                logger.LogInformation("Generated WEBP variant for {File}", request.FilePath);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "WEBP conversion failed for {File}", request.FilePath);
            }
        }
    }

    private readonly record struct ConversionRequest(string FilePath, string ContentType);
}
