using Vitarish.Sea.Application.Commands.Attachments;

namespace Vitarish.Sea.Infrastructure.Handlers;

internal class AddAttachmentHandler(IEntityRepository<Attachment, AttachmentDescriptor> repository) : IRequestHandler<AddAttachmentCommand>
{
    public async Task Handle(AddAttachmentCommand request, CancellationToken cancellationToken)
    {
        _ = await repository.AddAsync(new Attachment(
            request.EventId,
            request.Content,
            request.ContentType,
            request.ContentThumbnail,
            request.Size), cancellationToken);
    }
}
