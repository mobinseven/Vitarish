using Vitarish.Sea.Application.Commands.Attachments;
using Vitarish.Entities.Contracts.Models;

namespace Vitarish.Sea.Infrastructure.Handlers;

internal class AddAttachmentHandler(IEntityRepository<Attachment, AttachmentDescriptor> repository) : IRequestHandler<AddAttachmentCommand, EntityResult<Attachment>>
{
    public async Task<EntityResult<Attachment>> Handle(AddAttachmentCommand request, CancellationToken cancellationToken)
    {
        EntityResult<Attachment> attachment = Attachment.Create(
            request.EventId,
            request.Content,
            request.ContentType,
            request.ContentThumbnail,
            request.Size,
            request.Kind,
            request.FileName);
        if (attachment.IsFailure)
        {
            return attachment;
        }

        Attachment? added = await repository.AddAsync(attachment.Value!, cancellationToken);
        return added is null
            ? EntityResult<Attachment>.Failure(EntityError.Unknown("Attachment creation failed."))
            : EntityResult<Attachment>.Success(added);
    }
}
