namespace Vitarish.Sea.Application.Commands.Attachments;


public record AddAttachmentCommand(
    Guid EventId,
    string Content,
    string ContentType,
    string? ContentThumbnail,
    long Size
) : IRequest;
