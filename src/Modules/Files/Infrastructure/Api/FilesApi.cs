using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Vitarish.Files.Application.Abstractions;
using Vitarish.Modules.Aggregator.Core.Endpoints;

namespace Vitarish.Files.Infrastructure.Api;

public static class FilesApi
{
    public static IEndpointRouteBuilder MapFilesApi(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder filesGroup = endpoints.MapProtectedModuleGroup("/files");

        _ = filesGroup.MapModuleGet("/{id:guid}", GetFileByIdAsync, "FilesGetFileById", profile: ModuleEndpointProfile.BinaryDownload, contentType: ModuleEndpointOpenApiConventions.BinaryStream);
        _ = filesGroup.MapModuleHead("/{id:guid}", GetFileByIdAsync, "FilesGetFileByIdHead", contentType: ModuleEndpointOpenApiConventions.BinaryStream);

        return endpoints;
    }

    private static async Task GetFileByIdAsync(
        Guid id,
        HttpContext context,
        IObjectStorageService storageService,
         IFileInformationService fileInfoService)
    {
        FileInformationReadDto? fileInfo = await fileInfoService.GetAsync(id, context.RequestAborted).GetValueOrDefaultAsync();

        if (fileInfo is null)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        context.RequestAborted.ThrowIfCancellationRequested();

        bool exists = await storageService.ExistsAsync(fileInfo.Checksum, context.RequestAborted);

        if (!exists)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        ObjectMetadata metadata = await storageService.GetMetadataAsync(fileInfo.Checksum, context.RequestAborted);

        context.Response.ContentType = metadata.ContentType ?? fileInfo.ContentType ?? "application/octet-stream";
        context.Response.Headers.AcceptRanges = "bytes";

        if (HttpMethods.IsHead(context.Request.Method))
        {
            context.Response.ContentLength = metadata.Size;
            context.Response.StatusCode = StatusCodes.Status200OK;
            return;
        }

        (long offset, long length, bool isRangeRequest) = ParseRangeHeader(context.Request.Headers.Range, metadata.Size);

        if (isRangeRequest)
        {
            context.Response.StatusCode = StatusCodes.Status206PartialContent;
            context.Response.ContentLength = length;
            context.Response.Headers.ContentRange = $"bytes {offset}-{offset + length - 1}/{metadata.Size}";
        }
        else
        {
            context.Response.StatusCode = StatusCodes.Status200OK;
            context.Response.ContentLength = metadata.Size;
        }

        await storageService.StreamToAsync(
            fileInfo.Checksum,
            context.Response.Body,
            isRangeRequest ? offset : null,
            isRangeRequest ? length : null,
            context.RequestAborted);
    }

    private static (long offset, long length, bool isRangeRequest) ParseRangeHeader(string? rangeHeader, long fileSize)
    {
        if (string.IsNullOrWhiteSpace(rangeHeader))
        {
            return (0, fileSize, false);
        }

        string range = rangeHeader.Trim();
        if (!range.StartsWith("bytes=", StringComparison.OrdinalIgnoreCase))
        {
            return (0, fileSize, false);
        }

        string rangeValue = range[6..];
        string[] parts = rangeValue.Split('-');
        if (parts.Length != 2)
        {
            return (0, fileSize, false);
        }

        long start = 0;
        long end = fileSize - 1;

        if (!string.IsNullOrWhiteSpace(parts[0]) && long.TryParse(parts[0], out long parsedStart))
        {
            start = parsedStart;
        }

        if (!string.IsNullOrWhiteSpace(parts[1]) && long.TryParse(parts[1], out long parsedEnd))
        {
            end = parsedEnd;
        }

        start = Math.Max(0, Math.Min(start, fileSize - 1));
        end = Math.Max(start, Math.Min(end, fileSize - 1));
        long length = end - start + 1;

        return (start, length, true);
    }
}
