namespace Vitarish.Sea.Application.Commands.Attachments;

using Vitarish.Entities.Contracts.Models;

public record AddAttachmentCommand(
    Guid EventId,
    string Content,
    string ContentType,
    string? ContentThumbnail,
    long Size,
    AttachmentKind Kind,
    string? FileName
) : IRequest<EntityResult<Attachment>>;
