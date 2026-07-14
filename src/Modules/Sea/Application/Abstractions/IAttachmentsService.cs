
using Vitarish.Entities.Server.Abstractions;
using Vitarish.Entities.Contracts.Models;
using Vitarish.Sea.Domain.Enums;

namespace Vitarish.Sea.Application.Abstractions;

public interface IAttachmentsService : IEntityService<AttachmentReadDto, AttachmentWriteDto, AttachmentDescriptorDto>
{
    Task<IReadOnlyList<AttachmentReadDto>> GetByEventAsync(Guid eventId, CancellationToken cancellationToken);
    Task<int> GetCountByEventAsync(Guid eventId, CancellationToken cancellationToken);

    Task<EntityResult<AttachmentReadDto>> AddAsync(
        Guid eventId,
        string content,
        string contentType,
        string? contentThumbnail,
        long size,
        string? fileName,
        AttachmentKind kind,
        CancellationToken cancellationToken);

    Task<EntityResult<IReadOnlyList<AttachmentReadDto>>> AddBatchAsync(
        IReadOnlyList<AttachmentWriteDto> attachments,
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
