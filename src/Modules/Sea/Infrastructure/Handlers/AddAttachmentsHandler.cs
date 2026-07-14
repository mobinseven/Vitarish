using Vitarish.Sea.Application.Commands.Attachments;
using Vitarish.Entities.Contracts.Models;

namespace Vitarish.Sea.Infrastructure.Handlers;

internal class AddAttachmentsHandler(IEntityRepository<Attachment, AttachmentDescriptor> repository) : IRequestHandler<AddAttachmentsCommand, EntityResult<IReadOnlyList<Attachment>>>
{
    public async Task<EntityResult<IReadOnlyList<Attachment>>> Handle(AddAttachmentsCommand request, CancellationToken cancellationToken)
    {
        List<Attachment> validated = new(request.Attachments.Count);
        foreach (AttachmentItem item in request.Attachments)
        {
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
                return EntityResult<IReadOnlyList<Attachment>>.Failure([.. attachment.Errors]);
            }
            validated.Add(attachment.Value!);
        }

        List<Attachment> added = new(validated.Count);
        foreach (Attachment attachment in validated)
        {
            Attachment? entity = await repository.AddAsync(attachment, cancellationToken);
            if (entity is null)
            {
                return EntityResult<IReadOnlyList<Attachment>>.Failure(EntityError.Unknown("Attachment batch creation failed."));
            }
            added.Add(entity);
        }
        return EntityResult<IReadOnlyList<Attachment>>.Success(added);
    }
}
