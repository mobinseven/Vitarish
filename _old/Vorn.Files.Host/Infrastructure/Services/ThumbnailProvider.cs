namespace Vorn.Files.Host.Infrastructure.Services;

using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Threading.Tasks;
using Vorn.Files.Host.Domain.Options;
using Vorn.Files.Host.Infrastructure.Extensions;

public class ThumbnailProvider(RequestDelegate next, VornOptions options)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Query.TryGetValue("thumb", out Microsoft.Extensions.Primitives.StringValues value) || value != "1")
        {
            await next(context);
            return;
        }

        string? fileName = Path.GetFileName(context.Request.Path);
        if (string.IsNullOrWhiteSpace(fileName))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        string basePath = Path.Combine(Environment.CurrentDirectory, "wwwroot", options.Files.UploadPath);
        string thumbPath = Path.Combine(basePath, fileName) + ".thumb.jpg";
        if (!File.Exists(thumbPath))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        context.Response.ContentType = Thumbnails.ThumbnailContentType;
        await context.Response.SendFileAsync(thumbPath);
    }
}
