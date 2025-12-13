namespace Vitarish.Sea.Application.Commands.Attachments;


public record RemoveAttachmentCommand(
    Guid EventId,
    Guid AttachmentId
) : IRequest;
