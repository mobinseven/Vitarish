namespace Vorn.Files.Host.Infrastructure.Services;

using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Vorn.Files.Host.Domain.Options;
using Vorn.Files.Host.Infrastructure.Extensions;
using Vorn.Files.Host.Infrastructure.Interfaces;

public class ThumbnailProvider(
    RequestDelegate next,
    VornOptions options,
    IObjectStorageService storage)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Query.TryGetValue("thumb", out Microsoft.Extensions.Primitives.StringValues value) || value != "1")
        {
            await next(context);
            return;
        }

        if (!context.Items.TryGetValue("FileName", out object? fileNameObj) || fileNameObj is not string fileName)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        string objectKey = options.Files.BuildObjectKey(fileName);
        string thumbKey = objectKey + ".thumb.jpg";

        if (!await storage.ExistsAsync(thumbKey, context.RequestAborted).ConfigureAwait(false))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        ObjectMetadata metadata = await storage.GetMetadataAsync(thumbKey, context.RequestAborted).ConfigureAwait(false);
        context.Response.ContentType = metadata.ContentType ?? Thumbnails.ThumbnailContentType;
        context.Response.ContentLength = metadata.Size;

        await storage.StreamToAsync(thumbKey, context.Response.Body, cancellationToken: context.RequestAborted).ConfigureAwait(false);
    }
}
