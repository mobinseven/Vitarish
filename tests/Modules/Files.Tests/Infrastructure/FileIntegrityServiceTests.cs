using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Vitarish.Files.Application.Abstractions;
using Vitarish.Files.Domain.Entities;
using Vitarish.Files.Domain.Models;
using Vitarish.Files.Infrastructure.Data;
using Vitarish.Files.Infrastructure.Services;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Requests.Domain.Entities;

namespace Files.Tests.Infrastructure;

public sealed class FileIntegrityServiceTests
{
    [Fact]
    public async Task VerifyAsync_quarantines_changed_stored_content()
    {
        byte[] original = "verified bytes"u8.ToArray();
        byte[] changed = "altered bytes"u8.ToArray();
        string digest = Convert.ToHexString(SHA256.HashData(original)).ToLowerInvariant();
        Guid fileId = Guid.Parse("ee95d0ce-8fb3-4c33-8d82-a2f74b55dc91");
        const string tenantId = "tenant-a";
        FileInformationReadDto file = new()
        {
            Id = fileId,
            Owner = tenantId,
            Checksum = digest,
            ContentType = "application/pdf"
        };
        MemoryObjectStorage storage = new();
        storage.Set(FileStorageKey.For(tenantId, fileId), original, file.ContentType);

        await using FilesDbContext dbContext = CreateContext(tenantId);
        FileIntegrityService service = new(dbContext, storage);

        _ = await service.BeginEnrollmentAsync(file, original.Length, file.ContentType);
        (await service.VerifyAsync(file)).IsVerified.Should().BeTrue();

        storage.Set(FileStorageKey.For(tenantId, fileId), changed, file.ContentType);
        FileIntegrityVerificationResult result = await service.VerifyAsync(file);

        result.State.Should().Be(FileIntegrityState.Unavailable);
        result.ObjectKey.Should().BeNull();
    }

    [Fact]
    public async Task VerifyAsync_rejects_a_missing_legacy_object()
    {
        byte[] content = "legacy bytes"u8.ToArray();
        string digest = Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();
        FileInformationReadDto file = new()
        {
            Id = Guid.Parse("1dfbfdb8-9016-4aea-930a-fa2a0741c0eb"),
            Owner = "tenant-a",
            Checksum = digest,
            ContentType = "application/pdf",
            Metadata = new FileMetadata { Size = content.Length }
        };

        await using FilesDbContext dbContext = CreateContext(file.Owner);
        FileIntegrityService service = new(dbContext, new MemoryObjectStorage());

        _ = await service.BeginLegacyEnrollmentAsync(file);
        FileIntegrityVerificationResult result = await service.VerifyAsync(file);

        result.State.Should().Be(FileIntegrityState.Unavailable);
    }

    [Fact]
    public async Task VerifyAsync_rejects_a_missing_integrity_record()
    {
        byte[] content = "unrecorded bytes"u8.ToArray();
        Guid fileId = Guid.Parse("bc4e97d2-5dc4-41de-847d-bce2ef178f78");
        const string tenantId = "tenant-a";
        FileInformationReadDto file = new()
        {
            Id = fileId,
            Owner = tenantId,
            Checksum = Convert.ToHexString(SHA256.HashData(content)),
            ContentType = "application/pdf"
        };
        MemoryObjectStorage storage = new();
        storage.Set(FileStorageKey.For(tenantId, fileId), content, file.ContentType);

        await using FilesDbContext dbContext = CreateContext(tenantId);
        FileIntegrityService service = new(dbContext, storage);

        FileIntegrityVerificationResult result = await service.VerifyAsync(file);

        result.State.Should().Be(FileIntegrityState.Unavailable);
        result.ObjectKey.Should().BeNull();
    }

    [Fact]
    public async Task VerifyAsync_propagates_provider_failure_without_marking_record_verified()
    {
        byte[] content = "provider failure"u8.ToArray();
        Guid fileId = Guid.Parse("f1e8e9cf-78c6-4dd4-bfb2-dc8f1cb3b0cc");
        const string tenantId = "tenant-a";
        FileInformationReadDto file = new()
        {
            Id = fileId,
            Owner = tenantId,
            Checksum = Convert.ToHexString(SHA256.HashData(content)),
            ContentType = "application/pdf"
        };
        ThrowingObjectStorage storage = new();

        await using FilesDbContext dbContext = CreateContext(tenantId);
        FileIntegrityService service = new(dbContext, storage);
        _ = await service.BeginEnrollmentAsync(file, content.Length, file.ContentType);

        Func<Task> verify = () => service.VerifyAsync(file);

        await verify.Should().ThrowAsync<IOException>();
        (await dbContext.Set<FileIntegrityRecord>().SingleAsync()).State.Should().Be(FileIntegrityState.Pending);
    }

    [Fact]
    public async Task VerifyAsync_quarantines_an_object_with_substituted_metadata()
    {
        byte[] content = "verified bytes"u8.ToArray();
        Guid fileId = Guid.Parse("b05d05b9-905c-4bea-8f2a-8a76cbb27afc");
        const string tenantId = "tenant-a";
        FileInformationReadDto file = new()
        {
            Id = fileId,
            Owner = tenantId,
            Checksum = Convert.ToHexString(SHA256.HashData(content)),
            ContentType = "application/pdf"
        };
        MemoryObjectStorage storage = new();
        storage.Set(FileStorageKey.For(tenantId, fileId), content, "text/plain");

        await using FilesDbContext dbContext = CreateContext(tenantId);
        FileIntegrityService service = new(dbContext, storage);

        _ = await service.BeginEnrollmentAsync(file, content.Length, file.ContentType);
        FileIntegrityVerificationResult result = await service.VerifyAsync(file);

        result.State.Should().Be(FileIntegrityState.Unavailable);
        result.ObjectKey.Should().BeNull();
    }

    [Fact]
    public async Task VerifyAsync_quarantines_oversized_legacy_content_before_provider_access()
    {
        byte[] content = "oversized declaration"u8.ToArray();
        Guid fileId = Guid.Parse("0720cb2e-f37b-47b8-969d-531027278562");
        const string tenantId = "tenant-a";
        FileInformationReadDto file = new()
        {
            Id = fileId,
            Owner = tenantId,
            Checksum = Convert.ToHexString(SHA256.HashData(content)),
            ContentType = "application/pdf"
        };
        MemoryObjectStorage storage = new();

        await using FilesDbContext dbContext = CreateContext(tenantId);
        _ = await dbContext.Set<FileIntegrityRecord>().AddAsync(new FileIntegrityRecord
        {
            FileInformationId = fileId,
            TenantId = tenantId,
            ObjectKey = FileStorageKey.For(tenantId, fileId),
            Digest = file.Checksum,
            ContentType = file.ContentType,
            ContentLength = FileTransferPolicy.MaximumContentBytes + 1
        });
        _ = await dbContext.SaveChangesAsync();
        FileIntegrityService service = new(dbContext, storage);

        FileIntegrityVerificationResult result = await service.VerifyAsync(file);

        result.State.Should().Be(FileIntegrityState.Unavailable);
        storage.ExistsCallCount.Should().Be(0);
    }

    private static FilesDbContext CreateContext(string? tenantId) => new(
        new DbContextOptionsBuilder<FilesDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options,
        [],
        new TestRequestContext(tenantId),
        new HttpContextAccessor());

    private sealed class TestRequestContext(string? tenantId) : IRequestContext
    {
        public string Authority => "test";
        public string? ClientId => null;
        public string? OwnerId => null;
        public string? UserId => null;
        public string? TenantId => tenantId;
        public string? ResourceId => null;
        public string? TraceId => null;
        public string? RequestId => null;
        public string? AdditionalInfo => null;
        public System.Security.Claims.ClaimsPrincipal? User => null;
        public Request GetRequest() => new() { TenantId = TenantId };
    }

    private class MemoryObjectStorage : IObjectStorageService
    {
        private readonly Dictionary<string, StoredObject> objects = new(StringComparer.Ordinal);
        public int ExistsCallCount { get; private set; }

        public void Set(string key, byte[] content, string contentType) => objects[key] = new(content, contentType);
        public Task EnsureBucketAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UploadAsync(string objectKey, string contentType, Stream data, long size, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public virtual Task<bool> ExistsAsync(string objectKey, CancellationToken cancellationToken = default)
        {
            ExistsCallCount++;
            return Task.FromResult(objects.ContainsKey(objectKey));
        }
        public Task<ObjectMetadata> GetMetadataAsync(string objectKey, CancellationToken cancellationToken = default)
        {
            StoredObject stored = objects[objectKey];
            return Task.FromResult(new ObjectMetadata(stored.Content.Length, stored.ContentType));
        }
        public Task<Stream> DownloadAsync(string objectKey, CancellationToken cancellationToken = default) => Task.FromResult<Stream>(new MemoryStream(objects[objectKey].Content, writable: false));
        public async Task StreamToAsync(string objectKey, Stream destination, long? offset = null, long? length = null, CancellationToken cancellationToken = default)
        {
            StoredObject stored = objects[objectKey];
            await destination.WriteAsync(stored.Content, cancellationToken);
        }
        public Task RemoveAsync(string objectKey, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RenameAsync(string objectKey, string newObjectKey, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<ObjectListResult> ListAsync(string? prefix = null, string? continuationToken = null, int? maxKeys = null, CancellationToken cancellationToken = default) => Task.FromResult(new ObjectListResult([], null, false));
        public Task<string> GetPresignedUrlAsync(string objectKey, TimeSpan expiresIn, string httpMethod = "GET", CancellationToken cancellationToken = default) => Task.FromResult(string.Empty);
        private sealed record StoredObject(byte[] Content, string ContentType);
    }

    private sealed class ThrowingObjectStorage : MemoryObjectStorage
    {
        public override Task<bool> ExistsAsync(string objectKey, CancellationToken cancellationToken = default) =>
            Task.FromException<bool>(new IOException("provider unavailable"));
    }
}
