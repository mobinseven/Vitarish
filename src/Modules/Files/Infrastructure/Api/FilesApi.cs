using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Minio;
using Vitarish.Entities.Server.Models;
using Vitarish.Files.Application.Abstractions;
using Vitarish.Files.Domain.Options;
using Vitarish.Files.Infrastructure.Extensions;
using Vitarish.Files.Infrastructure.Interfaces;

namespace Vitarish.Files.Infrastructure.Api;

public static class FilesApi
{
    public static IEndpointRouteBuilder MapFilesApi(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder filesGroup = endpoints.MapGroup("/files");

        _ = filesGroup.MapGet("/{id:guid}", GetFileByIdAsync);
        _ = filesGroup.MapMethods("/{id:guid}", ["HEAD"], GetFileByIdAsync);
        _ = filesGroup.MapGet("/name/{**fileName}", GetFileByNameAsync);
        _ = filesGroup.MapMethods("/name/{**fileName}", ["HEAD"], GetFileByNameAsync);

        return endpoints;
    }

    private static async Task GetFileByIdAsync(
        Guid id,
        HttpContext context,
        IMinioClient minioClient,
        IObjectStorageService storageService,
         IFileInformationService fileInfoService,
        IOptionsSnapshot<FilesOptions> filesOptions)
    {
        FileInformationReadDto? fileInfo = await fileInfoService.GetAsync(id, context.RequestAborted).GetValueOrDefaultAsync();
        if (fileInfo is null)
        {
            EntityResult<IReadOnlyList<FileInformationReadDto>> listResult = await fileInfoService.GetListAsync(
                new() { Tag = id.ToString() },
                context.RequestAborted);
            fileInfo = listResult.Value?.FirstOrDefault();
        }

        if (fileInfo is null)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }


        await ServeFileAsync(fileInfo, context, storageService, filesOptions.Value);

    }

    private static async Task GetFileByNameAsync(
        string fileName,
        HttpContext context,
        IObjectStorageService storageService,
        IFileInformationService fileInfoService,
        IOptionsSnapshot<FilesOptions> filesOptions)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        string extractedId = FilesExtensions.ExtractId(fileName, filesOptions.Value.Prefix);
        if (!Guid.TryParse(extractedId, out Guid fileId))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }
        FileInformationReadDto? fileInfo = await fileInfoService.GetAsync(fileId, context.RequestAborted).GetValueOrDefaultAsync();

        if (fileInfo is null)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        await ServeFileAsync(fileInfo, context, storageService, filesOptions.Value);
    }

    private static async Task ServeFileAsync(
        FileInformationReadDto fileInfo,
        HttpContext context,
        IObjectStorageService storageService,
        FilesOptions filesSettings)
    {
        string objectKey = filesSettings.BuildObjectKey(fileInfo.Id.ToString());

        context.RequestAborted.ThrowIfCancellationRequested();

        bool exists = await storageService.ExistsAsync(objectKey, context.RequestAborted);

        if (!exists)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        ObjectMetadata metadata = await storageService.GetMetadataAsync(objectKey, context.RequestAborted);

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
            objectKey,
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
