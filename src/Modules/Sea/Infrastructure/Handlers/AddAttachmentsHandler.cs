using Vitarish.Sea.Application.Commands.Attachments;

namespace Vitarish.Sea.Infrastructure.Handlers;

internal class AddAttachmentsHandler(IEntityRepository<Attachment, AttachmentDescriptor> repository) : IRequestHandler<AddAttachmentsCommand>
{
    public async Task Handle(AddAttachmentsCommand request, CancellationToken cancellationToken)
    {
        foreach (AttachmentItem item in request.Attachments)
        {
            _ = await repository.AddAsync(new Attachment
            {
                EventId = request.EventId,
                Content = item.Content,
                ContentType = item.ContentType,
                ContentThumbnail = item.ContentThumbnail,
                Size = item.Size
            }, cancellationToken);
        }
    }
}

