namespace Vitarish.Sea.Application.Abstractions;

using Vitarish.Sea.Domain.Dto;

public interface IAttachmentsService : IEntityService<AttachmentDto, AttachmentWriteDto, AttachmentDescriptorDto>
{
    Task<IReadOnlyList<AttachmentDto>> GetByEventAsync(Guid eventId, CancellationToken cancellationToken);
    Task<int> GetCountByEventAsync(Guid eventId, CancellationToken cancellationToken);

    Task<AttachmentDto?> AddAsync(
        Guid eventId,
        string content,
        string contentType,
        string? contentThumbnail,
        long size,
        CancellationToken cancellationToken);

    Task AddAsync(
        Guid eventId,
        IEnumerable<AttachmentDto> attachments,
        CancellationToken cancellationToken);

    Task RemoveAsync(
        Guid eventId,
        Guid attachmentId,
        CancellationToken cancellationToken);

    Task<AttachmentDto?> UpdateAsync(
        Guid eventId,
        Guid attachmentId,
        string content,
        CancellationToken cancellationToken);

    Task<AttachmentSnapshot?> CaptureSnapshotAsync(Guid id, CancellationToken cancellationToken);
}
