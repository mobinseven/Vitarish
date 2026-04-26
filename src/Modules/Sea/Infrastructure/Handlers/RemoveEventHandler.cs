namespace Vitarish.Sea.Infrastructure.Handlers;

internal class RemoveEventHandler(IEntityRepository<Event, EventDescriptor> repository) : IRequestHandler<RemoveEventCommand, bool>
{
    public async Task<bool> Handle(RemoveEventCommand request, CancellationToken cancellationToken)
    {
        IReadOnlyList<Event> events = await repository.GetAsync(new EventDescriptor { ProcedureId = request.ProcedureId }, cancellationToken);
        Dictionary<Guid, Event> map = events.ToDictionary(static e => e.Id);

        if (!map.TryGetValue(request.EventId, out Event? current))
        {
            return false;
        }

        Event? previous = EventChainUtilities.FindPrevious(events, current, map);
        Event? next = EventChainUtilities.FindNext(events, current.Id);

        if (next is not null)
        {
            next.PreviousEventId = previous?.Id;
            _ = await repository.UpdateAsync(next, cancellationToken);
        }

        await repository.RemoveAsync(current, cancellationToken);
        return true;
    }
}
