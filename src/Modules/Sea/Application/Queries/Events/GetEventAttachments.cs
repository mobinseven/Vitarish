using Vitarish.Sea.Domain.Entities;
using Vitarish.Entities.Server.Core;

namespace Vitarish.Sea.Application.Queries.Events;

public record GetEventAttachmentsQuery(Guid EventId) : IRequest<IReadOnlyList<Attachment>>;
