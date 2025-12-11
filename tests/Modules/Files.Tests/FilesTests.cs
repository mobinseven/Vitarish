
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Minio;
using System.Collections.Concurrent;
using System.Net;
using System.Security.Cryptography;
using System.Threading.Channels;
using Vitarish.Files.Application.Abstractions;
using Vitarish.Files.Infrastructure.Interfaces;
using Vitarish.Files.Infrastructure.Services;
using Vorn.Entities.Models;

namespace Vitarish.Files.Tests;

public sealed class FilesHostTests
{
    [Fact]
    public async Task SaveFile_UploadsStreamAndStoresMetadata()
    {
        byte[] payload = "Hello, storage!"u8.ToArray();
        string checksum = Convert.ToHexString(SHA256.HashData(payload)).ToLowerInvariant();

        IOptions<FilesOptions> options = CreateOptions();
        InMemoryFileInformationService fileInformationService = new();
        RecordingObjectStorageService storage = new();
        TestTenantAccessor tenantAccessor = new("tenant-123");

        FilesService service = new(
            options,
            fileInformationService,
            NullLogger<FilesService>.Instance,
            storage,
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
        Assert.StartsWith($"{options.Value.UploadPath.Trim('/')}/{options.Value.Prefix}-", upload.ObjectKey, StringComparison.Ordinal);

        FileInformationDto stored = Assert.Single(fileInformationService.Stored);
        Assert.Equal(result.Url, stored.Url);
        Assert.Contains(upload.ObjectKey.Split('/').Last(), result.Url, StringComparison.Ordinal);
        Assert.Null(result.Thumbnail);
    }

    private static IOptions<FilesOptions> CreateOptions() => Options.Create(new FilesOptions()
    {
        Prefix = "files",
        BaseUrl = "https://files.example",
        UploadPath = "uploads"
    });

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
        private readonly List<FileInformationDto> store = [];
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
                query = byName.TryGetValue(descriptor.Name, out FileInformationDto? match) ? (new[] { match }) : Array.Empty<FileInformationDto>();
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

        private static string? TryResolveName(FileInformationDto dto) => string.IsNullOrWhiteSpace(dto.Url) ? null : dto.Url.Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
    }

    private sealed class RecordingObjectStorageService : IObjectStorageService
    {
        private readonly ConcurrentDictionary<string, (byte[] Data, string ContentType)> objects = new(StringComparer.OrdinalIgnoreCase);

        public readonly List<UploadRecord> Uploads = [];
        public readonly List<StreamRecord> Streams = [];

        public void Seed(string objectKey, string contentType, byte[] data) => objects[objectKey] = (data, contentType);

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

        public Task<ObjectMetadata> GetMetadataAsync(string objectKey, CancellationToken cancellationToken = default) => !objects.TryGetValue(objectKey, out (byte[] Data, string ContentType) value)
                ? throw new InvalidOperationException($"Object '{objectKey}' was not seeded.")
                : Task.FromResult(new ObjectMetadata(value.Data.Length, value.ContentType));

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

        public List<HttpRequestMessage> Requests { get; } = [];

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

        public StubHttpClientFactory(HttpClient client)
        {
            this.client = client;
        }

        public HttpClient CreateClient(string name) => client;
    }

}
