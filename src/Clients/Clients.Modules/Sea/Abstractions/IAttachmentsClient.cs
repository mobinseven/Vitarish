using Vitarish.Sea.Domain.Dto;

namespace Vitarish.Clients.Modules.Sea.Abstractions;

public interface IAttachmentsClient
{
    Task<IReadOnlyList<AttachmentDto>> GetByEventAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<int> GetCountByEventAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<AttachmentDto?> AddAsync(Guid eventId, string content, string contentType, string? contentThumbnail, long size, CancellationToken cancellationToken = default);
    Task AddAsync(Guid eventId, IEnumerable<AttachmentDto> attachments, CancellationToken cancellationToken = default);
    Task RemoveAsync(Guid eventId, Guid attachmentId, CancellationToken cancellationToken = default);
    Task<AttachmentDto?> UpdateAsync(Guid eventId, Guid attachmentId, string content, CancellationToken cancellationToken = default);
    Task<AttachmentSnapshot?> CaptureSnapshotAsync(Guid id, CancellationToken cancellationToken = default);
}
