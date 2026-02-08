using System.Collections.Concurrent;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Vitarish.Files.Application.Abstractions;
using Vitarish.Files.Infrastructure.Interfaces;
using Vitarish.Entities.Server.Models;

namespace Vitarish.Files.Tests;

/// <summary>
/// A test tenant accessor that allows specifying a tenant for tests.
/// </summary>
public sealed class TestTenantAccessor : IMultiTenantContextAccessor<TenantInfo>, IMultiTenantContextSetter
{
    public TestTenantAccessor(string? tenantId)
    {
        current = tenantId == null
            ? new MultiTenantContext<TenantInfo>()
            : new MultiTenantContext<TenantInfo>
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

/// <summary>
/// An in-memory implementation of IFileInformationService for testing.
/// </summary>
public sealed class InMemoryFileInformationService : IFileInformationService
{
    private readonly List<FileInformationDto> store = [];
    private readonly ConcurrentDictionary<string, FileInformationDto> byName = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<FileInformationDto> Stored => store;

    public void Seed(string fileName, FileInformationDto info)
    {
        store.Add(info);
        byName[fileName] = info;
    }

    public Task<EntityResult<FileInformationDto>> AddAsync(FileInformationWriteDto dto, CancellationToken cancellationToken)
    {
        FileInformationDto readDto = new()
        {
            Id = dto.Id,
            AllowedOrigins = dto.AllowedOrigins,
            ContentType = dto.ContentType,
            Thumbnail = dto.Thumbnail,
            Checksum = dto.Checksum,
            Url = dto.Url,
            Owner = dto.Owner,
            Tag = dto.Tag
        };
        store.Add(readDto);
        if (TryResolveName(readDto) is { } name)
        {
            byName[name] = readDto;
        }

        return Task.FromResult(EntityResult<FileInformationDto>.Success(readDto));
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
            if (byName.TryGetValue(descriptor.Name, out var match))
            {
                query = query.Where(i => i == match);
            }
            else
            {
                query = [];
            }
        }

        IReadOnlyList<FileInformationDto> result = query.ToList();
        return Task.FromResult(EntityResult<IReadOnlyList<FileInformationDto>>.Success(result));
    }

    public Task<EntityResult<IReadOnlyList<FileInformationDto>>> AddAsync(IReadOnlyList<FileInformationWriteDto> dtos, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<EntityResult<FileInformationDto>> UpdateAsync(FileInformationWriteDto dto, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<EntityResult<IReadOnlyList<FileInformationDto>>> UpdateAsync(IReadOnlyList<FileInformationWriteDto> dtos, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<EntityResult<FileInformationDto>> AddOrUpdateAsync(FileInformationWriteDto dto, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<EntityResult> RemoveAsync(FileInformationWriteDto dto, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<EntityResult> RemoveAsync(IReadOnlyList<FileInformationWriteDto> dtos, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<EntityResult> RemoveAsync(Guid id, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<EntityResult> RemoveByDescriptorAsync(FileInformationDescriptorDto descriptor, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<EntityResult<FileInformationDto>> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        FileInformationDto? match = store.FirstOrDefault(i => i.Id == id);
        return match is not null
            ? Task.FromResult(EntityResult<FileInformationDto>.Success(match))
            : Task.FromResult(EntityResult<FileInformationDto>.Failure(EntityError.NotFound("Not found")));
    }

    public Task<EntityResult<int>> CountAsync(FileInformationDescriptorDto descriptor, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<EntityResult<PartedResult<FileInformationDto>>> GetPartedAsync(FileInformationDescriptorDto descriptor, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    private static string? TryResolveName(FileInformationDto dto) => string.IsNullOrWhiteSpace(dto.Url) ? (string?)null : dto.Url.Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
}

/// <summary>
/// A recording implementation of IObjectStorageService for testing.
/// </summary>
public sealed class RecordingObjectStorageService : IObjectStorageService
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

