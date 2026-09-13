using Vitarish.Files.Domain.Models;

namespace Vitarish.Files.Application.Abstractions;

/// <summary>
/// Application-owned access boundary for the Files metadata resource.
/// </summary>
public interface IFileMetadataAccess
{
    Task<FileInformationReadDto?> GetAsync(Guid fileId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FileInformationReadDto>> ListAsync(
        FileInformationDescriptorDto descriptor,
        CancellationToken cancellationToken = default);

    Task<FileInformationReadDto?> CreateAsync(
        FileInformationWriteDto file,
        CancellationToken cancellationToken = default);

    Task<FileInformationReadDto?> AuthorizeDownloadAsync(
        Guid fileId,
        CancellationToken cancellationToken = default);
}
