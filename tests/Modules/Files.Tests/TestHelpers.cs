using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using System.Collections.Concurrent;
using Vitarish.Entities.Contracts;
using Vitarish.Files.Application.Abstractions;

namespace Files.Tests;

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
    private readonly List<FileInformationReadDto> store = [];
    private readonly ConcurrentDictionary<string, FileInformationReadDto> byName = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<FileInformationReadDto> Stored => store;

    public void Seed(string fileName, FileInformationReadDto info)
    {
        store.Add(info);
        byName[fileName] = info;
    }

    public Task<EntityResult<FileInformationReadDto>> AddAsync(FileInformationWriteDto dto, CancellationToken cancellationToken)
    {
        FileInformationReadDto readDto = new()
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

        return Task.FromResult(EntityResult<FileInformationReadDto>.Success(readDto));
    }

    public Task<EntityResult<IReadOnlyList<FileInformationReadDto>>> GetListAsync(FileInformationDescriptorDto descriptor, CancellationToken cancellationToken)
    {
        IEnumerable<FileInformationReadDto> query = store;

        if (!string.IsNullOrWhiteSpace(descriptor.Checksum))
        {
            query = query.Where(info => string.Equals(info.Checksum, descriptor.Checksum, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(descriptor.Name))
        {
            query = byName.TryGetValue(descriptor.Name, out FileInformationReadDto? match) ? query.Where(i => i == match) : [];
        }

        if (!string.IsNullOrWhiteSpace(descriptor.Tag))
        {
            query = query.Where(info => string.Equals(info.Tag, descriptor.Tag, StringComparison.OrdinalIgnoreCase));
        }

        IReadOnlyList<FileInformationReadDto> result = [.. query];
        return Task.FromResult(EntityResult<IReadOnlyList<FileInformationReadDto>>.Success(result));
    }

    public Task<EntityResult<IReadOnlyList<FileInformationReadDto>>> AddAsync(IReadOnlyList<FileInformationWriteDto> dtos, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<EntityResult<FileInformationReadDto>> UpdateAsync(FileInformationWriteDto dto, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<EntityResult<IReadOnlyList<FileInformationReadDto>>> UpdateAsync(IReadOnlyList<FileInformationWriteDto> dtos, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<EntityResult<FileInformationReadDto>> AddOrUpdateAsync(FileInformationWriteDto dto, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<EntityResult> RemoveAsync(FileInformationWriteDto dto, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<EntityResult> RemoveAsync(IReadOnlyList<FileInformationWriteDto> dtos, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<EntityResult> RemoveAsync(Guid id, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<EntityResult> RemoveByDescriptorAsync(FileInformationDescriptorDto descriptor, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<EntityResult<FileInformationReadDto>> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        FileInformationReadDto? match = store.FirstOrDefault(i => i.Id == id);
        return match is not null
            ? Task.FromResult(EntityResult<FileInformationReadDto>.Success(match))
            : Task.FromResult(EntityResult<FileInformationReadDto>.Failure(EntityError.NotFound("Not found")));
    }

    public Task<EntityResult<int>> CountAsync(FileInformationDescriptorDto descriptor, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<EntityResult<PartedResult<FileInformationReadDto>>> GetPartedAsync(FileInformationDescriptorDto descriptor, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    private static string? TryResolveName(FileInformationReadDto dto) => string.IsNullOrWhiteSpace(dto.Url) ? null : dto.Url.Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
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

    public Task RenameAsync(string objectKey, string newObjectKey, CancellationToken cancellationToken = default)
    {
        if (!objects.TryGetValue(objectKey, out (byte[] Data, string ContentType) value))
        {
            throw new InvalidOperationException($"Object '{objectKey}' was not seeded.");
        }

        objects[newObjectKey] = value;
        _ = objects.TryRemove(objectKey, out _);
        return Task.CompletedTask;
    }

    public Task<ObjectListResult> ListAsync(string? prefix = null, string? continuationToken = null, int? maxKeys = null, CancellationToken cancellationToken = default)
    {
        IEnumerable<ObjectListItem> items = objects
            .Where(kvp => prefix is null || kvp.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .Select(kvp => new ObjectListItem(kvp.Key, kvp.Value.Data.Length, DateTime.UtcNow, null));
        if (maxKeys is int takeCount && takeCount > 0)
        {
            items = items.Take(takeCount);
        }

        return Task.FromResult(new ObjectListResult([.. items], null, false));
    }

    public Task<string> GetPresignedUrlAsync(string objectKey, TimeSpan expiresIn, string httpMethod = "GET", CancellationToken cancellationToken = default) =>
        Task.FromResult($"https://example.local/{objectKey}");

    public readonly record struct UploadRecord(string ObjectKey, string ContentType, byte[] Data);

    public readonly record struct StreamRecord(string ObjectKey, long? Offset, long? Length);
}
