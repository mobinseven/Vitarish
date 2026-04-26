namespace Vitarish.Sea.Infrastructure.Handlers;

internal class RepairEventChainHandler(IEntityRepository<Event, EventDescriptor> repository) : IRequestHandler<RepairEventChainCommand>
{
    public async Task Handle(RepairEventChainCommand request, CancellationToken cancellationToken)
    {
        IReadOnlyList<Event> events = await repository.GetAsync(new EventDescriptor { ProcedureId = request.ProcedureId }, cancellationToken);
        if (events.Count <= 1)
        {
            return;
        }

        IReadOnlyList<Event> ordered = EventChainUtilities.OrderByChain(events);

        Guid? previous = null;
        List<Event> updates = [];

        foreach (Event current in ordered)
        {
            if (current.PreviousEventId != previous)
            {
                current.PreviousEventId = previous;
                updates.Add(current);
            }

            previous = current.Id;
        }

        if (updates.Count > 0)
        {
            await repository.UpdateRangeAsync(updates, cancellationToken);
        }
    }
}
