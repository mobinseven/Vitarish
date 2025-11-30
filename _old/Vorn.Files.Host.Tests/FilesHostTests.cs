namespace Vorn.Files.Host.Tests;

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Channels;
using System.Net.Http;
using System.Net;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Configuration;
using Minio;
using Vorn.Entities.Models;
using Vorn.Files.Core.Domain.Models;
using Vorn.Files.Core.Presentation.Interfaces;
using Vorn.Files.Host.Domain.Options;
using Vorn.Files.Host.Infrastructure.Interfaces;
using Vorn.Files.Host.Infrastructure.Services;

public sealed class FilesHostTests
{
    [Fact]
    public async Task SaveFile_UploadsStreamAndStoresMetadata()
    {
        byte[] payload = "Hello, storage!"u8.ToArray();
        string checksum = Convert.ToHexString(SHA256.HashData(payload)).ToLowerInvariant();

        VornOptions options = CreateOptions();
        InMemoryFileInformationService fileInformationService = new();
        RecordingObjectStorageService storage = new();
        RecordingThumbnailService thumbnails = new();
        RecordingWebpConversionService webp = new();
        TestTenantAccessor tenantAccessor = new("tenant-123");

        FilesService service = new(
            options,
            fileInformationService,
            NullLogger<FilesService>.Instance,
            storage,
            thumbnails,
            webp,
            tenantAccessor);

        Channel<byte[]> channel = Channel.CreateUnbounded<byte[]>();
        await channel.Writer.WriteAsync(payload);
        channel.Writer.Complete();

        FileInformationDto request = new()
        {
            AllowedOrigins = "https://app.example",
            ContentType = "image/png",
            Checksum = checksum,
            Metadata = new FileMetadata()
        };

        FileInformationDto? result = await service.SaveFile(channel.Reader, request);

        Assert.NotNull(result);
        Assert.Equal("tenant-123", result.Owner);
        Assert.NotNull(result.Url);
        Assert.Equal("png", result.Metadata.Extension);
        Assert.True(result.Metadata.Size >= payload.Length);

        RecordingObjectStorageService.UploadRecord upload = Assert.Single(storage.Uploads);
        Assert.Equal("image/png", upload.ContentType);
        Assert.Equal(payload, upload.Data);
        Assert.StartsWith($"{options.Files.UploadPath.Trim('/')}/{options.Files.Prefix}-", upload.ObjectKey, StringComparison.Ordinal);

        FileInformationDto stored = Assert.Single(fileInformationService.Stored);
        Assert.Equal(result.Url, stored.Url);
        Assert.Contains(upload.ObjectKey.Split('/').Last(), result.Url, StringComparison.Ordinal);

        string objectKey = upload.ObjectKey;
        Assert.Equal(objectKey, Assert.Single(thumbnails.Keys));
        Assert.Equal(objectKey, Assert.Single(webp.Keys));
    }

    [Fact]
    public async Task FileLoadPipeline_AllowsConfiguredOriginAndStreamsFile()
    {
        byte[] payload = new byte[] { 1, 2, 3, 4 };
        VornOptions options = CreateOptions();
        TestTenantAccessor tenantAccessor = new("tenant-123");

        InMemoryFileInformationService fileInformationService = new();
        FileInformationDto storedInfo = new()
        {
            AllowedOrigins = "https://app.example",
            ContentType = "application/octet-stream",
            Checksum = "abc123",
            Url = $"{options.Files.BaseUrl.TrimEnd('/')}/{options.Files.UploadPath}/test.bin",
            Metadata = new FileMetadata { Extension = "bin" }
        };
        fileInformationService.Seed("test.bin", storedInfo);

        ServiceCollection services = new();
        services.AddSingleton<IFileInformationService>(fileInformationService);
        IServiceProvider provider = services.BuildServiceProvider();
        IServiceScopeFactory scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

        MinioOptions minioOptions = CreateMinioOptions(prefix: options.Files.Prefix);
        ProxyMessageHandler handler = new(payload, storedInfo.ContentType);
        HttpClient proxyClient = new(handler);
        IMinioClient minioClient = new MinioClient()
            .WithEndpoint(minioOptions.Endpoint, minioOptions.Port)
            .WithCredentials(minioOptions.AccessKey, minioOptions.SecretKey)
            .Build();

        ObjectStorageFileMiddleware objectMiddleware = new(
            _ => Task.CompletedTask,
            options,
            minioOptions,
            minioClient,
            new StubHttpClientFactory(proxyClient),
            NullLogger<ObjectStorageFileMiddleware>.Instance);

        FileLoadCors corsMiddleware = new(
            ctx => objectMiddleware.InvokeAsync(ctx),
            options,
            scopeFactory,
            tenantAccessor);

        DefaultHttpContext context = new();
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = $"/{options.Files.UploadPath}/test.bin";
        context.Request.Headers["Origin"] = "https://app.example";
        context.Response.Body = new MemoryStream();

        await corsMiddleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        Assert.Equal(storedInfo.ContentType, context.Response.ContentType);
        Assert.Equal("*", context.Response.Headers["Access-Control-Allow-Origin"].Single());
        Assert.Equal("GET", context.Response.Headers["Access-Control-Allow-Methods"].Single());
        Assert.Equal("bytes", context.Response.Headers.AcceptRanges);

        context.Response.Body.Position = 0;
        byte[] streamed = ((MemoryStream)context.Response.Body).ToArray();
        Assert.Equal(payload, streamed);

        HttpRequestMessage request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Uri requestUri = request.RequestUri!;
        string expectedPath = string.Join(
            '/',
            new[] { string.Empty, minioOptions.BucketName }
                .Concat(new[] { minioOptions.ObjectPrefix, options.Files.UploadPath.Trim('/'), "test.bin" }
                    .Where(part => !string.IsNullOrWhiteSpace(part))));

        Assert.Equal(minioOptions.UseSsl ? Uri.UriSchemeHttps : Uri.UriSchemeHttp, requestUri.Scheme);
        Assert.Equal(minioOptions.Endpoint, requestUri.Host);
        if (!(minioOptions.UseSsl && minioOptions.Port == 443) && !(!minioOptions.UseSsl && minioOptions.Port == 80))
        {
            Assert.Equal(minioOptions.Port, requestUri.Port);
        }
        Assert.Equal(expectedPath, requestUri.AbsolutePath);
        Assert.False(string.IsNullOrEmpty(requestUri.Query));
        Assert.Contains("X-Amz-Signature", requestUri.Query);

        Assert.True(context.Items.ContainsKey(typeof(FileInformationDto)));
        Assert.Same(storedInfo, context.Items[typeof(FileInformationDto)]);
    }

    private static VornOptions CreateOptions() => new()
    {
        Files = new FilesOptions
        {
            Prefix = "files",
            BaseUrl = "https://files.example",
            UploadPath = "uploads"
        },
        Aaas = new AaasOptions { Authority = "https://authority" },
    };

    private static MinioOptions CreateMinioOptions(string endpoint = "localhost", int port = 9000, bool useSsl = false, string bucket = "bucket", string? prefix = null)
    {
        string connectionString = $"Endpoint={endpoint};Port={port};UseSsl={useSsl};AccessKey=test;SecretKey=test;BucketName={bucket}";
        if (!string.IsNullOrWhiteSpace(prefix))
        {
            connectionString += $";ObjectPrefix={prefix}";
        }

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Minio"] = connectionString
            })
            .Build();

        return new MinioOptions(configuration);
    }

    private sealed class TestTenantAccessor : IMultiTenantContextAccessor<TenantInfo>, IMultiTenantContextSetter
    {
        public TestTenantAccessor(string tenantId)
        {
            current = new MultiTenantContext<TenantInfo>
            {
                TenantInfo = new TenantInfo
                {
                    Id = tenantId,
                    Identifier = tenantId
                }
            };
        }

        private MultiTenantContext<TenantInfo> current;

        IMultiTenantContext<TenantInfo> IMultiTenantContextAccessor<TenantInfo>.MultiTenantContext => current;

        IMultiTenantContext IMultiTenantContextAccessor.MultiTenantContext => current;

        IMultiTenantContext? IMultiTenantContextSetter.MultiTenantContext
        {
            set => current = value as MultiTenantContext<TenantInfo> ?? new MultiTenantContext<TenantInfo>();
        }
    }

    private sealed class InMemoryFileInformationService : IFileInformationService
    {
        private readonly List<FileInformationDto> store = new();
        private readonly ConcurrentDictionary<string, FileInformationDto> byName = new(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyList<FileInformationDto> Stored => store;

        public void Seed(string fileName, FileInformationDto info)
        {
            store.Add(info);
            byName[fileName] = info;
        }

        public Task<EntityResult<FileInformationDto>> AddAsync(FileInformationDto dto, CancellationToken cancellationToken)
        {
            store.Add(dto);
            if (TryResolveName(dto) is { } name)
            {
                byName[name] = dto;
            }

            return Task.FromResult(EntityResult<FileInformationDto>.Success(dto));
        }

        public Task<EntityResult<IReadOnlyList<FileInformationDto>>> GetListAsync(FileInformationDescriptorDto descriptor, CancellationToken cancellationToken)
        {
            IEnumerable<FileInformationDto> query = store;

            if (!string.IsNullOrWhiteSpace(descriptor.Checksum))
            {
                query = query.Where(info => string.Equals(info.Checksum, descriptor.Checksum, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(descriptor.Name))
            {
                if (byName.TryGetValue(descriptor.Name, out FileInformationDto? match))
                {
                    query = new[] { match };
                }
                else
                {
                    query = Array.Empty<FileInformationDto>();
                }
            }

            IReadOnlyList<FileInformationDto> result = query.ToList();
            return Task.FromResult(EntityResult<IReadOnlyList<FileInformationDto>>.Success(result));
        }

        public Task<EntityResult<IReadOnlyList<FileInformationDto>>> AddAsync(IReadOnlyList<FileInformationDto> dtos, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<EntityResult<FileInformationDto>> UpdateAsync(FileInformationDto dto, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<EntityResult<IReadOnlyList<FileInformationDto>>> UpdateAsync(IReadOnlyList<FileInformationDto> dtos, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<EntityResult<FileInformationDto>> AddOrUpdateAsync(FileInformationDto dto, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<EntityResult> RemoveAsync(FileInformationDto dto, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<EntityResult> RemoveAsync(IReadOnlyList<FileInformationDto> dtos, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<EntityResult> RemoveAsync(Guid id, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<EntityResult> RemoveByDescriptorAsync(FileInformationDescriptorDto descriptor, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<EntityResult<FileInformationDto>> GetAsync(Guid id, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<EntityResult<int>> CountAsync(FileInformationDescriptorDto descriptor, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<EntityResult<PartedResult<FileInformationDto>>> GetPartedAsync(FileInformationDescriptorDto descriptor, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        private static string? TryResolveName(FileInformationDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Url))
            {
                return null;
            }

            return dto.Url.Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
        }
    }

    private sealed class RecordingObjectStorageService : IObjectStorageService
    {
        private readonly ConcurrentDictionary<string, (byte[] Data, string ContentType)> objects = new(StringComparer.OrdinalIgnoreCase);

        public readonly List<UploadRecord> Uploads = new();
        public readonly List<StreamRecord> Streams = new();

        public void Seed(string objectKey, string contentType, byte[] data)
        {
            objects[objectKey] = (data, contentType);
        }

        public Task EnsureBucketAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public async Task UploadAsync(string objectKey, string contentType, Stream data, long size, CancellationToken cancellationToken = default)
        {
            using MemoryStream buffer = new();
            await data.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
            byte[] bytes = buffer.ToArray();
            objects[objectKey] = (bytes, contentType);
            Uploads.Add(new UploadRecord(objectKey, contentType, bytes));
        }

        public Task<bool> ExistsAsync(string objectKey, CancellationToken cancellationToken = default) =>
            Task.FromResult(objects.ContainsKey(objectKey));

        public Task<ObjectMetadata> GetMetadataAsync(string objectKey, CancellationToken cancellationToken = default)
        {
            if (!objects.TryGetValue(objectKey, out (byte[] Data, string ContentType) value))
            {
                throw new InvalidOperationException($"Object '{objectKey}' was not seeded.");
            }

            return Task.FromResult(new ObjectMetadata(value.Data.Length, value.ContentType));
        }

        public Task<Stream> DownloadAsync(string objectKey, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public async Task StreamToAsync(
            string objectKey,
            Stream destination,
            long? offset = null,
            long? length = null,
            CancellationToken cancellationToken = default)
        {
            if (!objects.TryGetValue(objectKey, out (byte[] Data, string ContentType) value))
            {
                throw new InvalidOperationException($"Object '{objectKey}' was not seeded.");
            }

            long start = offset ?? 0;
            long count = length ?? (value.Data.Length - start);
            await destination.WriteAsync(value.Data.AsMemory((int)start, (int)count), cancellationToken).ConfigureAwait(false);
            Streams.Add(new StreamRecord(objectKey, offset, length));
        }

        public Task RemoveAsync(string objectKey, CancellationToken cancellationToken = default)
        {
            _ = objects.TryRemove(objectKey, out _);
            return Task.CompletedTask;
        }

        public readonly record struct UploadRecord(string ObjectKey, string ContentType, byte[] Data);

        public readonly record struct StreamRecord(string ObjectKey, long? Offset, long? Length);
    }

    private sealed class ProxyMessageHandler : HttpMessageHandler
    {
        private readonly byte[] payload;
        private readonly string contentType;

        public ProxyMessageHandler(byte[] payload, string contentType)
        {
            this.payload = payload;
            this.contentType = contentType;
        }

        public List<HttpRequestMessage> Requests { get; } = new();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);

            HttpResponseMessage response = new(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(payload)
            };

            response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
            response.Content.Headers.ContentLength = payload.Length;
            response.Headers.AcceptRanges.Add("bytes");

            return Task.FromResult(response);
        }
    }

    private sealed class StubHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient client;

        public StubHttpClientFactory(HttpClient client) => this.client = client;

        public HttpClient CreateClient(string name) => client;
    }

    private sealed class RecordingThumbnailService : IThumbnailService
    {
        public readonly List<string> Keys = new();

        public void Enqueue(string objectKey, string contentType) => Keys.Add(objectKey);
    }

    private sealed class RecordingWebpConversionService : IWebpConversionService
    {
        public readonly List<string> Keys = new();

        public void Enqueue(string objectKey, string contentType) => Keys.Add(objectKey);
    }
}
