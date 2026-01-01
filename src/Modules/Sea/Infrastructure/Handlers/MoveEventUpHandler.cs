namespace Vitarish.Sea.Infrastructure.Handlers;

internal class MoveEventUpHandler(IEntityRepository<Event, EventDescriptor> repository) : IRequestHandler<MoveEventUpCommand, bool>
{
    public async Task<bool> Handle(MoveEventUpCommand request, CancellationToken cancellationToken)
    {
        IReadOnlyList<Event> events = await repository.GetAsync(new EventDescriptor { ProcedureId = request.ProcedureId }, cancellationToken);
        Dictionary<Guid, Event> map = events.ToDictionary(static e => e.Id);

        if (!map.TryGetValue(request.EventId, out Event? current))
        {
            return false;
        }

        Event? previous = EventChainUtilities.FindPrevious(events, current, map);
        if (previous is null)
        {
            return false;
        }

        Event? previousPrevious = EventChainUtilities.FindPrevious(events, previous, map);
        Event? next = EventChainUtilities.FindNext(events, current.Id);

        current.PreviousEventId = previousPrevious?.Id;
        previous.PreviousEventId = current.Id;

        List<Event> updates = [current, previous];

        if (next is not null)
        {
            next.PreviousEventId = previous.Id;
            updates.Add(next);
        }

        await repository.UpdateRangeAsync(updates, cancellationToken);
        return true;
    }
}
