namespace Vitarish.Sea.Application.Abstractions;

using Vitarish.Entities.Server.Abstractions;
using Vitarish.Sea.Domain.Dto;

public interface IAttachmentsService : IEntityService<AttachmentReadDto, AttachmentWriteDto, AttachmentDescriptorDto>
{
    Task<IReadOnlyList<AttachmentReadDto>> GetByEventAsync(Guid eventId, CancellationToken cancellationToken);
    Task<int> GetCountByEventAsync(Guid eventId, CancellationToken cancellationToken);

    Task<AttachmentReadDto?> AddAsync(
        Guid eventId,
        string content,
        string contentType,
        string? contentThumbnail,
        long size,
        CancellationToken cancellationToken);

    Task AddAsync(
        Guid eventId,
        IEnumerable<AttachmentReadDto> attachments,
        CancellationToken cancellationToken);

    Task RemoveAsync(
        Guid eventId,
        Guid attachmentId,
        CancellationToken cancellationToken);

    Task<AttachmentReadDto?> UpdateAsync(
        Guid eventId,
        Guid attachmentId,
        string content,
        CancellationToken cancellationToken);

    Task<AttachmentSnapshot?> CaptureSnapshotAsync(Guid id, CancellationToken cancellationToken);
}
