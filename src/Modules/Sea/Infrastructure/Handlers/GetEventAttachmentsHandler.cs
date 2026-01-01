using Vitarish.Sea.Application.Queries.Events;

namespace Vitarish.Sea.Infrastructure.Handlers;

internal class GetEventAttachmentsHandler(IEntityRepository<Attachment, AttachmentDescriptor> repository)
    : IRequestHandler<GetEventAttachmentsQuery, IReadOnlyList<Attachment>>
{
    public async Task<IReadOnlyList<Attachment>> Handle(GetEventAttachmentsQuery request, CancellationToken cancellationToken) => await repository.GetAsync(new AttachmentDescriptor { EventId = request.EventId }, cancellationToken);
}
