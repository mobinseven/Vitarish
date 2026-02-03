using Vitarish.Sea.Application.Commands.Attachments;

namespace Vitarish.Sea.Infrastructure.Handlers;

internal class AddAttachmentsHandler(IEntityRepository<Attachment, AttachmentDescriptor> repository) : IRequestHandler<AddAttachmentsCommand>
{
    public async Task Handle(AddAttachmentsCommand request, CancellationToken cancellationToken)
    {
        foreach (AttachmentItem item in request.Attachments)
        {
            _ = await repository.AddAsync(new Attachment(
                request.EventId,
                item.Content,
                item.ContentType,
                item.ContentThumbnail,
                item.Size), cancellationToken);
        }
    }
}

