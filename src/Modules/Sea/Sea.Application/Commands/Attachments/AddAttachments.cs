namespace Vitarish.Sea.Application.Commands.Attachments;


public record AddAttachmentsCommand(Guid EventId, IEnumerable<AttachmentItem> Attachments) : IRequest;

public record AttachmentItem(
    string Content,
    string ContentType,
    string? ContentThumbnail,
    long Size);

