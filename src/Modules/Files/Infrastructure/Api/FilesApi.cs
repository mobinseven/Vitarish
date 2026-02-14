using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Minio;
using Minio.DataModel.Args;
using Vitarish.Entities.Server.Models;
using Vitarish.Files.Application.Abstractions;
using Vitarish.Files.Domain.Models;
using Vitarish.Files.Domain.Options;
using Vitarish.Files.Infrastructure.Extensions;

namespace Vitarish.Files.Infrastructure.Api;

public static class FilesApi
{
    private static readonly HashSet<string> HeaderExclusions = new(StringComparer.OrdinalIgnoreCase)
    {
        HeaderNames.Host,
        HeaderNames.Authorization,
        HeaderNames.Cookie,
    };

    public static IEndpointRouteBuilder MapFilesApi(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder filesGroup = endpoints.MapGroup("/files");

        filesGroup.MapGet("/{id:guid}", GetFileByIdAsync);
        filesGroup.MapMethods("/{id:guid}", new[] { "HEAD" }, GetFileByIdAsync);
        filesGroup.MapGet("/name/{**fileName}", GetFileByNameAsync);
        filesGroup.MapMethods("/name/{**fileName}", new[] { "HEAD" }, GetFileByNameAsync);

        return endpoints;
    }

    private static async Task GetFileByIdAsync(
        Guid id,
        HttpContext context,
        IMinioClient minioClient,
        IHttpClientFactory httpClientFactory,
        IMultiTenantContextAccessor<TenantInfo> tenantAccessor,
        IServiceScopeFactory serviceScopeFactory,
        IOptionsSnapshot<FilesOptions> filesOptions,
        IOptionsSnapshot<MinioOptions> minioOptions)
    {
        using IDisposable tenantScope = tenantAccessor.UseTenant(TenantDefaults.RootTenant);
        using IServiceScope scope = serviceScopeFactory.CreateScope();
        IFileInformationService fileInfoService = scope.ServiceProvider.GetRequiredService<IFileInformationService>();

        EntityResult<FileInformationDto> result = await fileInfoService.GetAsync(id, context.RequestAborted);
        FileInformationDto? fileInfo = result.Value;

        if (fileInfo is null)
        {
            EntityResult<IReadOnlyList<FileInformationDto>> listResult = await fileInfoService.GetListAsync(
                new() { Tag = id.ToString() }, 
                context.RequestAborted);
            fileInfo = listResult.Value?.FirstOrDefault();
        }

        if (fileInfo is null)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        await ServeFileAsync(fileInfo, id, context, minioClient, httpClientFactory, filesOptions.Value, minioOptions.Value);
    }

    private static async Task GetFileByNameAsync(
        string fileName,
        HttpContext context,
        IMinioClient minioClient,
        IHttpClientFactory httpClientFactory,
        IMultiTenantContextAccessor<TenantInfo> tenantAccessor,
        IServiceScopeFactory serviceScopeFactory,
        IOptionsSnapshot<FilesOptions> filesOptions,
        IOptionsSnapshot<MinioOptions> minioOptions)
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

        using IDisposable tenantScope = tenantAccessor.UseTenant(TenantDefaults.RootTenant);
        using IServiceScope scope = serviceScopeFactory.CreateScope();
        IFileInformationService fileInfoService = scope.ServiceProvider.GetRequiredService<IFileInformationService>();

        EntityResult<FileInformationDto> result = await fileInfoService.GetAsync(fileId, context.RequestAborted);
        FileInformationDto? fileInfo = result.Value;

        if (fileInfo is null)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        await ServeFileAsync(fileInfo, fileId, context, minioClient, httpClientFactory, filesOptions.Value, minioOptions.Value);
    }

    private static async Task ServeFileAsync(
        FileInformationDto fileInfo,
        Guid fileId,
        HttpContext context,
        IMinioClient minioClient,
        IHttpClientFactory httpClientFactory,
        FilesOptions filesSettings,
        MinioOptions minioSettings)
    {
        string? extension = fileInfo.Metadata.Extension;
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = ContentTypeExtensions.SafeGetExtension(fileInfo.ContentType);
        }

        string fileName = $"{filesSettings.Prefix}-{fileId:N}.{extension}";
        string objectKey = filesSettings.BuildObjectKey(fileName);

        context.RequestAborted.ThrowIfCancellationRequested();
        Uri proxyUri = await CreatePresignedUriAsync(minioClient, objectKey, minioSettings.BucketName);

        HttpMethod method = HttpMethods.IsHead(context.Request.Method) ? HttpMethod.Head : HttpMethod.Get;
        using HttpRequestMessage proxyRequest = new(method, proxyUri);
        CopyRequestHeaders(context.Request, proxyRequest);

        HttpClient proxyClient = httpClientFactory.CreateClient("FilesApi");
        using HttpResponseMessage proxyResponse = await proxyClient.SendAsync(
            proxyRequest,
            HttpCompletionOption.ResponseHeadersRead,
            context.RequestAborted);

        context.Response.StatusCode = (int)proxyResponse.StatusCode;
        CopyResponseHeaders(proxyResponse, context.Response);

        if (HttpMethods.IsHead(context.Request.Method) || proxyResponse.Content is null)
        {
            return;
        }

        await using Stream responseStream = await proxyResponse.Content.ReadAsStreamAsync(context.RequestAborted);
        await responseStream.CopyToAsync(context.Response.Body, context.RequestAborted);
    }

    private static async Task<Uri> CreatePresignedUriAsync(IMinioClient minioClient, string objectKey, string bucketName)
    {
        PresignedGetObjectArgs args = new PresignedGetObjectArgs()
            .WithBucket(bucketName)
            .WithObject(objectKey)
            .WithExpiry((int)TimeSpan.FromMinutes(60).TotalSeconds);

        string url = await minioClient.PresignedGetObjectAsync(args);
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
}
