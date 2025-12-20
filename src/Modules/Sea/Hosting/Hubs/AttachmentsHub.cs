using Vitarish.Entities.Server.Core;
using Vitarish.Entities.Server.Services;
using Vitarish.Entities.Server.SignalR;
using Vitarish.Sea.Application.Abstractions;
using Vitarish.Sea.Domain.Dto;

namespace Vitarish.Sea.Hosting.Hubs;

public sealed class AttachmentsHub(IAttachmentsService service, IEntityUserService? entityUserService = null)
    : EntityServer<AttachmentDto, AttachmentWriteDto, AttachmentDescriptorDto, IAttachmentsService>(service, entityUserService)
{
    public Task<IReadOnlyList<AttachmentDto>> GetByEventAsync(Guid eventId, CancellationToken cancellationToken) =>
        EntityService.GetByEventAsync(eventId, cancellationToken);

    public Task<int> GetCountByEventAsync(Guid eventId, CancellationToken cancellationToken) =>
        EntityService.GetCountByEventAsync(eventId, cancellationToken);

    public Task<AttachmentDto?> AddAttachmentAsync(Guid eventId, string content, string contentType, string? contentThumbnail, long size, CancellationToken cancellationToken) =>
        EntityService.AddAsync(eventId, content, contentType, contentThumbnail, size, cancellationToken);

    public Task AddAttachmentRangeAsync(Guid eventId, IEnumerable<AttachmentDto> attachments, CancellationToken cancellationToken) =>
        EntityService.AddAsync(eventId, attachments, cancellationToken);

    public Task RemoveAttachmentAsync(Guid eventId, Guid attachmentId, CancellationToken cancellationToken) =>
        EntityService.RemoveAsync(eventId, attachmentId, cancellationToken);

    public Task<AttachmentDto?> UpdateAttachmentContentAsync(Guid eventId, Guid attachmentId, string content, CancellationToken cancellationToken) =>
        EntityService.UpdateAsync(eventId, attachmentId, content, cancellationToken);

    public Task<AttachmentSnapshot?> CaptureSnapshotAsync(Guid id, CancellationToken cancellationToken) =>
        EntityService.CaptureSnapshotAsync(id, cancellationToken);
}
