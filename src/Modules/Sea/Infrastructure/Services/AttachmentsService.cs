using Vitarish.Sea.Application.Commands.Attachments;
using Vitarish.Sea.Application.Queries.Events;
using Vitarish.Entities.Contracts.Models;
using Vitarish.Sea.Domain.Enums;
using Vitarish.Sea.Domain.Mappers;

namespace Vitarish.Sea.Infrastructure.Services;

[RegisterTransient]
internal sealed class AttachmentsService(
    IMediator mediator)
    : EntityService<Attachment, AttachmentDescriptor, AttachmentReadDto, AttachmentWriteDto, AttachmentDescriptorDto>
    (
        mediator, attachment => attachment.ToRead(),
        attachment => attachment.ToEntity(),
        attachment => attachment.ToDescriptor()
    ),
    IAttachmentsService
{
    public async Task<IReadOnlyList<AttachmentReadDto>> GetByEventAsync(Guid eventId, CancellationToken cancellationToken) => (await GetListAsync<GetEventAttachmentsQuery, Attachment>(
                new GetEventAttachmentsQuery(eventId),
                attachment => attachment.ToRead(),
                cancellationToken)).EnsureValue("Get attachments by event");

    public async Task<int> GetCountByEventAsync(Guid eventId, CancellationToken cancellationToken) => (await CountAsync<Attachment, AttachmentDescriptor>(new() { EventId = eventId }, cancellationToken))
            .EnsureValue("Count attachments by event");

    public async Task<EntityResult<AttachmentReadDto>> AddAsync(
        Guid eventId,
        string content,
        string contentType,
        string? contentThumbnail,
        long size,
        string? fileName,
        AttachmentKind kind,
        CancellationToken cancellationToken)
    {
        EntityResult newContent = Attachment.ValidateNewContent(kind, content);
        if (newContent.IsFailure)
        {
            return EntityResult<AttachmentReadDto>.Failure([.. newContent.Errors]);
        }

        EntityResult<Attachment> attachment = Attachment.Create(
            eventId, content, contentType, contentThumbnail, size, kind, fileName);
        return attachment.IsFailure
            ? EntityResult<AttachmentReadDto>.Failure([.. attachment.Errors])
            : await base.AddAsync(attachment.Value!, cancellationToken);
    }

    public async Task<EntityResult<IReadOnlyList<AttachmentReadDto>>> AddBatchAsync(
        IReadOnlyList<AttachmentWriteDto> attachments,
        CancellationToken cancellationToken)
    {
        List<Attachment> entities = new(attachments.Count);
        foreach (AttachmentWriteDto item in attachments)
        {
            EntityResult newContent = Attachment.ValidateNewContent(item.Kind, item.Content);
            if (newContent.IsFailure)
            {
                return EntityResult<IReadOnlyList<AttachmentReadDto>>.Failure([.. newContent.Errors]);
            }

            EntityResult<Attachment> attachment = Attachment.Create(
                item.EventId,
                item.Content,
                item.ContentType,
                item.ContentThumbnail,
                item.Size,
                item.Kind,
                item.FileName);
            if (attachment.IsFailure)
            {
                return EntityResult<IReadOnlyList<AttachmentReadDto>>.Failure([.. attachment.Errors]);
            }
            entities.Add(attachment.Value!);
        }

        return await base.AddAsync(entities, cancellationToken);
    }

    public async Task RemoveAsync(Guid eventId, Guid attachmentId, CancellationToken cancellationToken) => await Mediator.Send(new RemoveAttachmentCommand(eventId, attachmentId), cancellationToken);

    public async Task<AttachmentReadDto?> UpdateAsync(
        Guid eventId,
        Guid attachmentId,
        string content,
        CancellationToken cancellationToken)
    {
        Attachment? entity = (await GetEntityAsync(attachmentId, cancellationToken)).GetValueOrDefault();
        if (entity is null || entity.EventId != eventId || Attachment.ValidateNewContent(entity.Kind, content).IsFailure)
        {
            return null;
        }

        entity.Content = content;

        Attachment? updated = await Mediator.Send(
            new UpdateEntityCommand<Attachment, AttachmentDescriptor>(entity),
            cancellationToken);
        return updated?.ToRead();
    }

    public async Task<AttachmentSnapshot?> CaptureSnapshotAsync(Guid id, CancellationToken cancellationToken)
    {
        AttachmentReadDto? attachment = (await GetAsync(id, cancellationToken)).GetValueOrDefault();
        return attachment is null ? null : new AttachmentSnapshot(attachment.Id, attachment.Content);
    }
}
