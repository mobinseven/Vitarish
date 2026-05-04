namespace Vitarish.Sea.Application.Queries.Events;

public record GetEventAttachmentsQuery(Guid EventId) : IRequest<IReadOnlyList<Attachment>>;
