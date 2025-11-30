using Microsoft.Net.Http.Headers;
using Minio;
using Minio.DataModel.Args;
using Vorn.Files.Host.Domain.Options;
using Vorn.Files.Host.Infrastructure.Extensions;

namespace Vorn.Files.Host.Infrastructure.Services;

public sealed class ObjectStorageFileMiddleware(
    RequestDelegate next,
    VornOptions options,
    MinioOptions minioOptions,
    IMinioClient minioClient,
    IHttpClientFactory httpClientFactory,
    ILogger<ObjectStorageFileMiddleware> logger)
{
    private readonly string bucketName = minioOptions.BucketName
        ?? throw new InvalidOperationException("Bucket name must be configured for object storage proxying.");
    private readonly string? objectPrefix = minioOptions.ObjectPrefix;
    private readonly IMinioClient minioClient = minioClient;

    public async Task InvokeAsync(HttpContext context)
    {
        if(!context.Request.Path.StartsWithSegments("/" + options.Files.UploadPath))
        {
            await next(context);
            return;
        }

        if(!HttpMethods.IsGet(context.Request.Method) && !HttpMethods.IsHead(context.Request.Method))
        {
            context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
            return;
        }

        string? fileName = context.Items.TryGetValue("FileName", out object? value) && value is string resolved
            ? resolved
            : Path.GetFileName(context.Request.Path);

        if(string.IsNullOrWhiteSpace(fileName))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        string objectKey = options.Files.BuildObjectKey(fileName);
        string resolvedKey = BuildObjectKey(objectKey);
        HttpMethod outboundMethod = HttpMethods.IsHead(context.Request.Method)
            ? HttpMethod.Get
            : new HttpMethod(context.Request.Method);
        Uri proxyUri = await CreatePresignedUriAsync(resolvedKey).ConfigureAwait(false);

        using HttpRequestMessage proxyRequest = new(outboundMethod, proxyUri);
        CopyRequestHeaders(context.Request, proxyRequest);

        try
        {
            HttpClient proxyClient = httpClientFactory.CreateClient(nameof(ObjectStorageFileMiddleware));
            using HttpResponseMessage proxyResponse = await proxyClient.SendAsync(
                    proxyRequest,
                    HttpCompletionOption.ResponseHeadersRead,
                    context.RequestAborted)
                .ConfigureAwait(false);

            context.Response.StatusCode = (int)proxyResponse.StatusCode;
            CopyResponseHeaders(proxyResponse, context.Response);

            if(HttpMethods.IsHead(context.Request.Method) || proxyResponse.Content is null)
            {
                return;
            }

            await using Stream responseStream = await proxyResponse.Content.ReadAsStreamAsync(context.RequestAborted)
                .ConfigureAwait(false);
            await responseStream.CopyToAsync(context.Response.Body, context.RequestAborted).ConfigureAwait(false);
        }
        catch(OperationCanceledException) when(context.RequestAborted.IsCancellationRequested)
        {
            throw;
        }
        catch(Exception ex)
        {
            logger.LogError(ex, "Failed to proxy object {ObjectKey}", resolvedKey);
            if(!context.Response.HasStarted)
            {
                context.Response.StatusCode = StatusCodes.Status502BadGateway;
            }
        }
    }

    private async Task<Uri> CreatePresignedUriAsync(string objectKey)
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
        foreach((string key, Microsoft.Extensions.Primitives.StringValues values) in source.Headers)
        {
            if(key.Equals(HeaderNames.Host, StringComparison.OrdinalIgnoreCase)
                || key.Equals(HeaderNames.Authorization, StringComparison.OrdinalIgnoreCase)
                || key.Equals(HeaderNames.Cookie, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if(!destination.Headers.TryAddWithoutValidation(key, (IEnumerable<string>)values))
            {
                destination.Content ??= new ByteArrayContent(Array.Empty<byte>());
                _ = destination.Content.Headers.TryAddWithoutValidation(key, (IEnumerable<string>)values);
            }
        }
    }

    private static void CopyResponseHeaders(HttpResponseMessage source, HttpResponse destination)
    {
        foreach((string key, IEnumerable<string> values) in source.Headers)
        {
            destination.Headers[key] = values.ToArray();
        }

        if(source.Content is not null)
        {
            foreach((string key, IEnumerable<string> values) in source.Content.Headers)
            {
                destination.Headers[key] = values.ToArray();
            }

            destination.ContentLength = source.Content.Headers.ContentLength is long contentLength ? contentLength : null;
        }

        _ = destination.Headers.Remove(HeaderNames.TransferEncoding);
        _ = destination.Headers.Remove(HeaderNames.Connection);
        _ = destination.Headers.Remove(HeaderNames.KeepAlive);
    }

    private string BuildObjectKey(string objectKey)
    {
        string trimmedKey = objectKey.TrimStart('/');
        string? prefix = objectPrefix?.Trim('/');
        return string.IsNullOrEmpty(prefix)
            ? trimmedKey
            : string.Join('/', prefix, trimmedKey).Replace("//", "/", StringComparison.Ordinal);
    }
}
