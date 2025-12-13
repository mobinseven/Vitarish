namespace Vitarish.Sea.Infrastructure.Handlers;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Vitarish.Sea.Application.Commands.Events;
using Vitarish.Sea.Domain.Entities;
using Vitarish.Sea.Domain.Models;
using Vitarish.Sea.Infrastructure.Extensions;

internal class RepairEventChainHandler(IEntityRepository<Event, EventDescriptor> repository) : IRequestHandler<RepairEventChainCommand>
{
    public async Task Handle(RepairEventChainCommand request, CancellationToken cancellationToken)
    {
        IReadOnlyList<Event> events = await repository.GetAsync(new EventDescriptor { ProcedureId = request.ProcedureId }, cancellationToken);
        if(events.Count <= 1)
        {
            return;
        }

        IReadOnlyList<Event> ordered = EventChainUtilities.OrderByChain(events);

        Guid? previous = null;
        List<Event> updates = [];

        foreach(Event current in ordered)
        {
            if(current.PreviousEventId != previous)
            {
                current.PreviousEventId = previous;
                updates.Add(current);
            }

            previous = current.Id;
        }

        if(updates.Count > 0)
        {
            await repository.UpdateRangeAsync(updates, cancellationToken);
        }
    }
}
