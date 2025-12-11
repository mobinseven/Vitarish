using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;
using Vitarish.Files.Domain.Options;
using Vitarish.Files.Infrastructure.Extensions;

namespace Vitarish.Files.Infrastructure.Services;

public sealed class ObjectStorageMiddleware(
    RequestDelegate next,
    IMinioClient minioClient,
    IHttpClientFactory httpClientFactory,
    ILogger<ObjectStorageMiddleware> logger)
{
    private readonly RequestDelegate _next = next;
    private readonly IMinioClient _minioClient = minioClient;
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly ILogger<ObjectStorageMiddleware> _logger = logger;

    // Headers intentionally excluded from proxy requests because object storage uses its own authentication.
    private static readonly HashSet<string> HeaderExclusions = new(StringComparer.OrdinalIgnoreCase)
    {
        HeaderNames.Host,
        HeaderNames.Authorization,
        HeaderNames.Cookie,
    };

    public async Task InvokeAsync(HttpContext context)
    {
        IOptionsSnapshot<FilesOptions> filesOptions = context.RequestServices.GetRequiredService<IOptionsSnapshot<FilesOptions>>();
        IOptionsSnapshot<MinioOptions> minioOptions = context.RequestServices.GetRequiredService<IOptionsSnapshot<MinioOptions>>();

        FilesOptions filesSettings = filesOptions.Value;
        MinioOptions currentMinioOptions = minioOptions.Value;
        ValidateOptions(filesSettings, currentMinioOptions);

        if (!context.Request.Path.StartsWithSegments("/" + filesSettings.UploadPath))
        {
            await _next(context);
            return;
        }

        if (!HttpMethods.IsGet(context.Request.Method) && !HttpMethods.IsHead(context.Request.Method))
        {
            context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
            return;
        }

        string? fileName = ResolveFileName(context);

        if (string.IsNullOrWhiteSpace(fileName))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        string objectKey = filesSettings.BuildObjectKey(fileName);
        string resolvedKey = BuildObjectKey(objectKey, currentMinioOptions.ObjectPrefix);
        HttpMethod outboundMethod = HttpMethods.IsHead(context.Request.Method)
            ? HttpMethod.Head
            : HttpMethod.Get;

        try
        {
            context.RequestAborted.ThrowIfCancellationRequested();
            Uri proxyUri = await CreatePresignedUriAsync(resolvedKey, currentMinioOptions.BucketName).ConfigureAwait(false);
            using HttpRequestMessage proxyRequest = new(outboundMethod, proxyUri);
            CopyRequestHeaders(context.Request, proxyRequest);
            HttpClient proxyClient = _httpClientFactory.CreateClient(nameof(ObjectStorageMiddleware));
            using HttpResponseMessage proxyResponse = await proxyClient.SendAsync(
                    proxyRequest,
                    HttpCompletionOption.ResponseHeadersRead,
                    context.RequestAborted)
                .ConfigureAwait(false);

            context.Response.StatusCode = (int)proxyResponse.StatusCode;
            CopyResponseHeaders(proxyResponse, context.Response);

            if (HttpMethods.IsHead(context.Request.Method) || proxyResponse.Content is null)
            {
                return;
            }

            await using Stream responseStream = await proxyResponse.Content.ReadAsStreamAsync(context.RequestAborted)
                .ConfigureAwait(false);
            await responseStream.CopyToAsync(context.Response.Body, context.RequestAborted).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            throw;
        }
        catch (MinioException ex)
        {
            _logger.LogError(
                ex,
                "Failed to create presigned URL for {ObjectKey} ({FileName}) from {RequestPath}.",
                resolvedKey,
                fileName,
                context.Request.Path);
            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = StatusCodes.Status502BadGateway;
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(
                ex,
                "Failed to proxy object {ObjectKey} ({FileName}) from {RequestPath}.",
                resolvedKey,
                fileName,
                context.Request.Path);
            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = StatusCodes.Status502BadGateway;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected failure while proxying object {ObjectKey} ({FileName}) from {RequestPath}.",
                resolvedKey,
                fileName,
                context.Request.Path);
            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = StatusCodes.Status502BadGateway;
            }
        }
    }

    private async Task<Uri> CreatePresignedUriAsync(string objectKey, string bucketName)
    {
        PresignedGetObjectArgs args = new PresignedGetObjectArgs()
            .WithBucket(bucketName)
            .WithObject(objectKey)
            .WithExpiry((int)TimeSpan.FromMinutes(60).TotalSeconds);

        string url = await _minioClient.PresignedGetObjectAsync(args).ConfigureAwait(false);
        return new Uri(url);
    }

    // Performance-sensitive: this runs on every proxied request, so keep allocations minimal.
    private static void CopyRequestHeaders(HttpRequest source, HttpRequestMessage destination)
    {
        foreach ((string key, Microsoft.Extensions.Primitives.StringValues values) in source.Headers)
        {
            if (HeaderExclusions.Contains(key))
            {
                continue;
            }

            if (!destination.Headers.TryAddWithoutValidation(key, (IEnumerable<string>)values))
            {
                destination.Content ??= new ByteArrayContent(Array.Empty<byte>());
                _ = destination.Content.Headers.TryAddWithoutValidation(key, (IEnumerable<string>)values);
            }
        }
    }

    private static void CopyResponseHeaders(HttpResponseMessage source, HttpResponse destination)
    {
        foreach ((string key, IEnumerable<string> values) in source.Headers)
        {
            destination.Headers[key] = values.ToArray();
        }

        if (source.Content is not null)
        {
            foreach ((string key, IEnumerable<string> values) in source.Content.Headers)
            {
                destination.Headers[key] = values.ToArray();
            }

            destination.ContentLength = source.Content.Headers.ContentLength is long contentLength ? contentLength : null;
        }

        _ = destination.Headers.Remove(HeaderNames.TransferEncoding);
        _ = destination.Headers.Remove(HeaderNames.Connection);
        _ = destination.Headers.Remove(HeaderNames.KeepAlive);
    }

    private static void ValidateOptions(FilesOptions filesOptions, MinioOptions minioOptions)
    {
        if (string.IsNullOrWhiteSpace(filesOptions.UploadPath))
        {
            throw new InvalidOperationException("Upload path must be configured for object storage proxying.");
        }

        if (string.IsNullOrWhiteSpace(filesOptions.Prefix))
        {
            throw new InvalidOperationException("Files prefix must be configured for object storage proxying.");
        }

        if (string.IsNullOrWhiteSpace(minioOptions.BucketName)
            || string.IsNullOrWhiteSpace(minioOptions.Endpoint)
            || string.IsNullOrWhiteSpace(minioOptions.AccessKey)
            || string.IsNullOrWhiteSpace(minioOptions.SecretKey)
            || minioOptions.HttpRequestTimeoutSeconds <= 0)
        {
            throw new InvalidOperationException(
                "Minio options must include endpoint, credentials, bucket name, and a positive request timeout.");
        }
    }

    private static string? ResolveFileName(HttpContext context)
    {
        if (context.Items.TryGetValue("FileName", out object? value) && value is string resolvedFileName)
        {
            return resolvedFileName;
        }

        PathString path = context.Request.Path;
        return string.IsNullOrWhiteSpace(path.Value)
            ? null
            : Path.GetFileName(path);
    }

    private static string BuildObjectKey(string objectKey, string? objectPrefix)
    {
        string trimmedKey = objectKey.TrimStart('/');
        string? prefix = objectPrefix?.Trim('/');
        return string.IsNullOrEmpty(prefix)
            ? trimmedKey
            : string.Join('/', prefix, trimmedKey).Replace("//", "/", StringComparison.Ordinal);
    }
}
