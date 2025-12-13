namespace Vitarish.Sea.Infrastructure.Handlers;

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Vitarish.Sea.Application.Commands.Events;
using Vitarish.Sea.Domain.Entities;
using Vitarish.Sea.Domain.Models;
using Vitarish.Sea.Infrastructure.Extensions;

internal class RemoveEventHandler(IEntityRepository<Event, EventDescriptor> repository) : IRequestHandler<RemoveEventCommand, bool>
{
    public async Task<bool> Handle(RemoveEventCommand request, CancellationToken cancellationToken)
    {
        IReadOnlyList<Event> events = await repository.GetAsync(new EventDescriptor { ProcedureId = request.ProcedureId }, cancellationToken);
        Dictionary<Guid, Event> map = events.ToDictionary(static e => e.Id);

        if(!map.TryGetValue(request.EventId, out Event? current))
        {
            return false;
        }

        Event? previous = EventChainUtilities.FindPrevious(events, current, map);
        Event? next = EventChainUtilities.FindNext(events, current.Id);

        if(next is not null)
        {
            next.PreviousEventId = previous?.Id;
            await repository.UpdateAsync(next, cancellationToken);
        }

        await repository.RemoveAsync(current, cancellationToken);
        return true;
    }
}
