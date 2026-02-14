using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;
using Vitarish.Entities.Server.Models;
using Vitarish.Files.Application.Abstractions;
using Vitarish.Files.Domain.Models;
using Vitarish.Files.Domain.Options;
using Vitarish.Files.Infrastructure.Extensions;

namespace Vitarish.Files.Infrastructure.Services;

public sealed class FileByIdMiddleware(
    RequestDelegate next,
    IMinioClient minioClient,
    IHttpClientFactory httpClientFactory,
    ILogger<FileByIdMiddleware> logger)
{
    private const string FilesPrefix = "/files/";

    private static readonly HashSet<string> HeaderExclusions = new(StringComparer.OrdinalIgnoreCase)
    {
        HeaderNames.Host,
        HeaderNames.Authorization,
        HeaderNames.Cookie,
    };

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/files", out PathString remaining)
            || string.IsNullOrWhiteSpace(remaining.Value))
        {
            await next(context);
            return;
        }

        if (!HttpMethods.IsGet(context.Request.Method) && !HttpMethods.IsHead(context.Request.Method))
        {
            context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
            return;
        }

        string idSegment = remaining.Value.TrimStart('/');
        if (!Guid.TryParse(idSegment, out Guid fileId))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        IMultiTenantContextAccessor<TenantInfo> tenantAccessor =
            context.RequestServices.GetRequiredService<IMultiTenantContextAccessor<TenantInfo>>();
        using IDisposable tenantScope = tenantAccessor.UseTenant(TenantDefaults.RootTenant);

        using IServiceScope scope = context.RequestServices.GetRequiredService<IServiceScopeFactory>().CreateScope();
        IFileInformationService fileInfoService =
            scope.ServiceProvider.GetRequiredService<IFileInformationService>();

        EntityResult<FileInformationDto> result =
            await fileInfoService.GetAsync(fileId, context.RequestAborted);
        FileInformationDto? fileInfo = result.Value ?? (await fileInfoService.GetListAsync(new() { Tag = idSegment }, context.RequestAborted).GetValueOrDefaultAsync() ?? []).FirstOrDefault();

        if (fileInfo is null)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        IOptionsSnapshot<FilesOptions> filesOptions =
            context.RequestServices.GetRequiredService<IOptionsSnapshot<FilesOptions>>();
        IOptionsSnapshot<MinioOptions> minioOptions =
            context.RequestServices.GetRequiredService<IOptionsSnapshot<MinioOptions>>();

        FilesOptions filesSettings = filesOptions.Value;
        MinioOptions currentMinioOptions = minioOptions.Value;

        string? extension = fileInfo.Metadata.Extension;
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = ContentTypeExtensions.SafeGetExtension(fileInfo.ContentType);
        }

        string fileName = $"{filesSettings.Prefix}-{fileId:N}.{extension}";
        string objectKey = filesSettings.BuildObjectKey(fileName);
        string resolvedKey = BuildObjectKey(objectKey, currentMinioOptions.ObjectPrefix);

        HttpMethod outboundMethod = HttpMethods.IsHead(context.Request.Method)
            ? HttpMethod.Head
            : HttpMethod.Get;

        try
        {
            context.RequestAborted.ThrowIfCancellationRequested();
            Uri proxyUri = await CreatePresignedUriAsync(resolvedKey, currentMinioOptions.BucketName);
            using HttpRequestMessage proxyRequest = new(outboundMethod, proxyUri);
            CopyRequestHeaders(context.Request, proxyRequest);
            // Reuse the ObjectStorageMiddleware-configured HttpClient (same MinIO timeout settings)
            HttpClient proxyClient = httpClientFactory.CreateClient(nameof(ObjectStorageMiddleware));
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
            context.Response.StatusCode = StatusCodes.Status200OK;

            await using Stream responseStream = await proxyResponse.Content
                .ReadAsStreamAsync(context.RequestAborted)
                .ConfigureAwait(false);
            await responseStream.CopyToAsync(context.Response.Body, context.RequestAborted)
                .ConfigureAwait(false);
            return;
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            throw;
        }
        catch (MinioException ex)
        {
            logger.LogError(ex, "Failed to create presigned URL for file {FileId} ({ObjectKey}).", fileId, resolvedKey);
            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = StatusCodes.Status502BadGateway;
            }
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Failed to proxy file {FileId} ({ObjectKey}).", fileId, resolvedKey);
            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = StatusCodes.Status502BadGateway;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected failure while proxying file {FileId} ({ObjectKey}).", fileId, resolvedKey);
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

        string url = await minioClient.PresignedGetObjectAsync(args).ConfigureAwait(false);
        return new Uri(url);
    }

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

    private static string BuildObjectKey(string objectKey, string? objectPrefix)
    {
        string trimmedKey = objectKey.TrimStart('/');
        string? prefix = objectPrefix?.Trim('/');
        return string.IsNullOrEmpty(prefix)
            ? trimmedKey
            : string.Join('/', prefix, trimmedKey).Replace("//", "/", StringComparison.Ordinal);
    }
}
