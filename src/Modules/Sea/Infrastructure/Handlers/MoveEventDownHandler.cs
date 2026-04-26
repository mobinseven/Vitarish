namespace Vitarish.Sea.Infrastructure.Handlers;

internal class MoveEventDownHandler(IEntityRepository<Event, EventDescriptor> repository) : IRequestHandler<MoveEventDownCommand, bool>
{
    public async Task<bool> Handle(MoveEventDownCommand request, CancellationToken cancellationToken)
    {
        IReadOnlyList<Event> events = await repository.GetAsync(new EventDescriptor { ProcedureId = request.ProcedureId }, cancellationToken);
        Dictionary<Guid, Event> map = events.ToDictionary(static e => e.Id);

        if (!map.TryGetValue(request.EventId, out Event? current))
        {
            return false;
        }

        Event? next = EventChainUtilities.FindNext(events, current.Id);
        if (next is null)
        {
            return false;
        }

        Event? nextNext = EventChainUtilities.FindNext(events, next.Id);
        Event? previous = EventChainUtilities.FindPrevious(events, current, map);

        next.PreviousEventId = previous?.Id;
        current.PreviousEventId = next.Id;

        List<Event> updates = [next, current];

        if (nextNext is not null)
        {
            nextNext.PreviousEventId = current.Id;
            updates.Add(nextNext);
        }

        await repository.UpdateRangeAsync(updates, cancellationToken);
        return true;
    }
}
