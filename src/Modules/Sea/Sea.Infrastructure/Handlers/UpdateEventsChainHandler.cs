
using Vitarish.Sea.Application.Commands.Events;
using DomainEventDescriptor = Vitarish.Sea.Domain.Models.EventDescriptor;

namespace Vitarish.Sea.Infrastructure.Handlers;

internal class UpdateEventsChainHandler(IEntityRepository<Event, EventDescriptor> repository) : IRequestHandler<UpdateEventsChainCommand>
{
    public async Task Handle(UpdateEventsChainCommand request, CancellationToken cancellationToken)
    {
        IReadOnlyList<Event> all = await repository.GetAsync(new DomainEventDescriptor { ProcedureId = request.ProcedureId }, cancellationToken);
        Dictionary<Guid, Event> map = all.ToDictionary(e => e.Id);

        HashSet<Guid> ids = request.Events.Where(e => e.Id.HasValue).Select(e => e.Id!.Value).ToHashSet();
        List<Event> toRemove = all.Where(e => !ids.Contains(e.Id)).ToList();
        if(toRemove.Count > 0)
        {
            await repository.RemoveRangeAsync(toRemove, cancellationToken);
        }

        List<Event> updated = [];
        foreach(EventChainChange change in request.Events)
        {
            if(change.Id is null || !map.TryGetValue(change.Id.Value, out Event? entity))
            {
                continue;
            }

            if(entity.PreviousEventId == change.PreviousEventId)
            {
                continue;
            }

            entity.PreviousEventId = change.PreviousEventId;
            updated.Add(entity);
        }

        if(updated.Count > 0)
        {
            await repository.UpdateRangeAsync(updated, cancellationToken);
        }

        return;
    }
}

