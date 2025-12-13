namespace Vitarish.Sea.Infrastructure.Handlers;

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Vitarish.Sea.Application.Commands.Events;
using Vitarish.Sea.Domain.Entities;
using Vitarish.Sea.Domain.Models;
using Vitarish.Sea.Infrastructure.Extensions;

internal class AddEventAfterHandler(IEntityRepository<Event, EventDescriptor> repository) : IRequestHandler<AddEventAfterCommand, Event?>
{
    public async Task<Event?> Handle(AddEventAfterCommand request, CancellationToken cancellationToken)
    {
        IReadOnlyList<Event> events = await repository.GetAsync(new EventDescriptor { ProcedureId = request.ProcedureId }, cancellationToken);
        Dictionary<Guid, Event> map = events.ToDictionary(static e => e.Id);

        Event? after = request.AfterEventId is Guid afterId && map.TryGetValue(afterId, out Event? existing)
            ? existing
            : EventChainUtilities.FindLast(events);

        Event entity = new(request.ProjectId, request.ProcedureId, after?.Id);
        Event? added = await repository.AddAsync(entity, cancellationToken);
        if(added is null)
        {
            return null;
        }

        if(after is not null)
        {
            Event? next = EventChainUtilities.FindNext(events, after.Id);
            if(next is not null)
            {
                next.PreviousEventId = added.Id;
                await repository.UpdateAsync(next, cancellationToken);
            }
        }

        return added;
    }
}
