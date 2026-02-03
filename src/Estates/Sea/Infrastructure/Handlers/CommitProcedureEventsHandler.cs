namespace Vitarish.Sea.Infrastructure.Handlers;

internal class CommitProcedureEventsHandler(IEntityRepository<Event, EventDescriptor> repository)
    : IRequestHandler<CommitProcedureEventsCommand, int>
{
    public async Task<int> Handle(CommitProcedureEventsCommand request, CancellationToken cancellationToken)
    {
        IReadOnlyList<Event> events = await repository.GetAsync(new EventDescriptor { ProcedureId = request.ProcedureId }, cancellationToken);

        List<Event> pending = new(events.Count);
        foreach (Event entity in events)
        {
            if (entity.CommittedAt.HasValue)
            {
                continue;
            }

            entity.Commit(request.ReporterUserId, request.CommitTime);
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
