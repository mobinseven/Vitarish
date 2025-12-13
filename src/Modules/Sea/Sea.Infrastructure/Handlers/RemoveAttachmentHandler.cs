using Vitarish.Sea.Application.Commands.Attachments;

namespace Vitarish.Sea.Infrastructure.Handlers;

internal class RemoveAttachmentHandler(IEntityRepository<Attachment, AttachmentDescriptor> repository) : IRequestHandler<RemoveAttachmentCommand>
{
    public async Task Handle(RemoveAttachmentCommand request, CancellationToken cancellationToken)
    {
        Attachment? entity = await repository.GetAsync(request.AttachmentId, cancellationToken);
        if(entity is null)
        {
            return;
        }

        await repository.RemoveAsync(entity, cancellationToken);
    }
}
