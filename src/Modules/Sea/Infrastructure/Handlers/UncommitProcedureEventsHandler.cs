namespace Vitarish.Sea.Infrastructure.Handlers;

internal class UncommitProcedureEventsHandler(IEntityRepository<Event, EventDescriptor> repository)
    : IRequestHandler<UncommitProcedureEventsCommand, int>
{
    public async Task<int> Handle(UncommitProcedureEventsCommand request, CancellationToken cancellationToken)
    {
        IReadOnlyList<Event> events = await repository.GetAsync(new EventDescriptor { ProcedureId = request.ProcedureId }, cancellationToken);

        List<Event> pending = new(events.Count);
        foreach (Event entity in events)
        {
            if (!entity.CommittedAt.HasValue)
            {
                continue;
            }

            entity.Uncommit();
            pending.Add(entity);
        }

        if (pending.Count == 0)
        {
            return 0;
        }

        await repository.UpdateRangeAsync(pending, cancellationToken);
        return pending.Count;
    }
}
