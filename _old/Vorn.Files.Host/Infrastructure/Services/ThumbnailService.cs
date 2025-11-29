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

public sealed class ThumbnailService(ILogger<ThumbnailService> logger) : BackgroundService, IThumbnailService
{
    private readonly Channel<ThumbnailRequest> _channel = Channel.CreateUnbounded<ThumbnailRequest>();

    public void Enqueue(string filePath, string contentType) => _channel.Writer.TryWrite(new(filePath, contentType));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (ThumbnailRequest req in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                string thumbPath = req.FilePath + ".thumb.jpg";
                await using FileStream source = File.OpenRead(req.FilePath);
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
                await File.WriteAllBytesAsync(thumbPath, bytes, stoppingToken);
                logger.LogInformation("Generated thumbnail for {File}", req.FilePath);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Thumbnail generation failed for {File}", req.FilePath);
            }
        }
    }

    private record ThumbnailRequest(string FilePath, string ContentType);
}
