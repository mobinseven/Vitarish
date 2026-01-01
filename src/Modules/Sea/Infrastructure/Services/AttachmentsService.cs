using Vitarish.Sea.Application.Commands.Attachments;
using Vitarish.Sea.Application.Queries.Events;

namespace Vitarish.Sea.Infrastructure.Services;

[RegisterTransient]
internal sealed class AttachmentsService(
    IMediator mediator,
    AttachmentsMapper mapper)
    : EntityService<Attachment, AttachmentDescriptor, AttachmentDto, AttachmentWriteDto, AttachmentDescriptorDto>(mediator, mapper.ToDto, mapper.ToDomain, mapper.ToDomain), IAttachmentsService
{
    public async Task<IReadOnlyList<AttachmentDto>> GetByEventAsync(Guid eventId, CancellationToken cancellationToken) => (await GetListAsync<GetEventAttachmentsQuery, Attachment>(
                new GetEventAttachmentsQuery(eventId),
                mapper.ToDto,
                cancellationToken)).EnsureValue("Get attachments by event");

    public async Task<int> GetCountByEventAsync(Guid eventId, CancellationToken cancellationToken) => (await CountAsync<Attachment, AttachmentDescriptor>(new() { EventId = eventId }, cancellationToken))
            .EnsureValue("Count attachments by event");

    public async Task<AttachmentDto?> AddAsync(
        Guid eventId,
        string content,
        string contentType,
        string? contentThumbnail,
        long size,
        CancellationToken cancellationToken) => (await AddAsync(new Attachment(eventId, content, contentType, contentThumbnail, size),
             cancellationToken)).GetValueOrDefault();

    public async Task AddAsync(
        Guid eventId,
        IEnumerable<AttachmentDto> attachments,
        CancellationToken cancellationToken) => await Mediator.Send(
            new AddAttachmentsCommand(
                eventId,
                attachments.Select(a => new AttachmentItem(a.Content, a.ContentType, a.ContentThumbnail, a.Size))),
            cancellationToken);

    public async Task RemoveAsync(Guid eventId, Guid attachmentId, CancellationToken cancellationToken) => await Mediator.Send(new RemoveAttachmentCommand(eventId, attachmentId), cancellationToken);

    public async Task<AttachmentDto?> UpdateAsync(
        Guid eventId,
        Guid attachmentId,
        string content,
        CancellationToken cancellationToken)
    {
        Attachment? entity = (await GetEntityAsync(attachmentId, cancellationToken)).GetValueOrDefault();
        if (entity is null || entity.EventId != eventId)
        {
            return null;
        }

        entity.Content = content;

        Attachment? updated = await Mediator.Send(
            new UpdateEntityCommand<Attachment, AttachmentDescriptor>(entity),
            cancellationToken);
        return updated is null ? null : mapper.ToDto(updated);
    }

    public async Task<AttachmentSnapshot?> CaptureSnapshotAsync(Guid id, CancellationToken cancellationToken)
    {
        AttachmentDto? attachment = (await GetAsync(id, cancellationToken)).GetValueOrDefault();
        return attachment is null ? null : new AttachmentSnapshot(attachment.Id, attachment.Content);
    }
}
