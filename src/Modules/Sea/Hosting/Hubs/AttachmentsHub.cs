using Vitarish.Entities.Server.Core;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Entities.Server.Services;
using Vitarish.Entities.Server.SignalR;
using Vitarish.Sea.Application.Abstractions;
using Vitarish.Sea.Domain.Dto;

namespace Vitarish.Sea.Hosting.Hubs;

public sealed class AttachmentsHub : EntityServer<AttachmentDto, AttachmentWriteDto, AttachmentDescriptorDto, IAttachmentsService>
{
    private readonly IAttachmentsService _service;

    public AttachmentsHub(IServiceScopeFactory serviceScopeFactory, IAttachmentsService service, IEntityUserService? entityUserService = null)
        : base(serviceScopeFactory, entityUserService)
    {
        _service = service;
    }

    public Task<IReadOnlyList<AttachmentDto>> GetByEventAsync(Guid eventId, CancellationToken cancellationToken) =>
        _service.GetByEventAsync(eventId, cancellationToken);

    public Task<int> GetCountByEventAsync(Guid eventId, CancellationToken cancellationToken) =>
        _service.GetCountByEventAsync(eventId, cancellationToken);

    public Task<AttachmentDto?> AddAttachmentAsync(Guid eventId, string content, string contentType, string? contentThumbnail, long size, CancellationToken cancellationToken) =>
        _service.AddAsync(eventId, content, contentType, contentThumbnail, size, cancellationToken);

    public Task AddAttachmentRangeAsync(Guid eventId, IEnumerable<AttachmentDto> attachments, CancellationToken cancellationToken) =>
        _service.AddAsync(eventId, attachments, cancellationToken);

    public Task RemoveAttachmentAsync(Guid eventId, Guid attachmentId, CancellationToken cancellationToken) =>
        _service.RemoveAsync(eventId, attachmentId, cancellationToken);

    public Task<AttachmentDto?> UpdateAttachmentContentAsync(Guid eventId, Guid attachmentId, string content, CancellationToken cancellationToken) =>
        _service.UpdateAsync(eventId, attachmentId, content, cancellationToken);

    public Task<AttachmentSnapshot?> CaptureSnapshotAsync(Guid id, CancellationToken cancellationToken) =>
        _service.CaptureSnapshotAsync(id, cancellationToken);
}
