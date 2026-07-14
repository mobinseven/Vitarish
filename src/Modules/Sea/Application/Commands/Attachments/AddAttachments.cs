using Vitarish.Sea.Domain.Enums;
using Vitarish.Entities.Contracts.Models;

namespace Vitarish.Sea.Application.Commands.Attachments;


public record AddAttachmentsCommand(IReadOnlyList<AttachmentItem> Attachments) : IRequest<EntityResult<IReadOnlyList<Attachment>>>;

public record AttachmentItem(
    Guid EventId,
    string Content,
    string ContentType,
    string? ContentThumbnail,
    long Size,
    string? FileName,
    AttachmentKind Kind);
