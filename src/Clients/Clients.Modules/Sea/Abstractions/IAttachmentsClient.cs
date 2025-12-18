using Vitarish.Clients.Modules.Sea.Dto;
using Vitarish.Entities.Client.SignalR;

namespace Vitarish.Clients.Modules.Sea.Abstractions;

public interface IAttachmentsClient : IEntityClient<AttachmentDto, AttachmentDto, AttachmentDescriptorDto>
{
    Task<IReadOnlyList<AttachmentDto>> GetByEventAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<int> GetCountByEventAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<AttachmentDto?> AddAttachmentAsync(Guid eventId, string content, string contentType, string? contentThumbnail, long size, CancellationToken cancellationToken = default);
    Task AddAttachmentRangeAsync(Guid eventId, IEnumerable<AttachmentDto> attachments, CancellationToken cancellationToken = default);
    Task RemoveAttachmentAsync(Guid eventId, Guid attachmentId, CancellationToken cancellationToken = default);
    Task<AttachmentDto?> UpdateAttachmentContentAsync(Guid eventId, Guid attachmentId, string content, CancellationToken cancellationToken = default);
    Task<AttachmentSnapshot?> CaptureSnapshotAsync(Guid id, CancellationToken cancellationToken = default);
}
