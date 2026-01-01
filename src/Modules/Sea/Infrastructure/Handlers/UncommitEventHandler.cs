namespace Vitarish.Sea.Infrastructure.Handlers;

internal class UncommitEventHandler(IEntityRepository<Event, EventDescriptor> repository) : IRequestHandler<UncommitEventCommand>
{
    public async Task Handle(UncommitEventCommand request, CancellationToken cancellationToken)
    {
        Event? entity = await repository.GetAsync(request.EventId, cancellationToken);
        if (entity is null)
        {
            return;
        }

        entity.Uncommit();
        _ = await repository.UpdateAsync(entity, cancellationToken);
    }
}

