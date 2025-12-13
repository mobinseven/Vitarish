

using Vitarish.Sea.Application.Commands.Events;

namespace Vitarish.Sea.Infrastructure.Handlers;

internal class CommitEventHandler(IEntityRepository<Event, EventDescriptor> repository) : IRequestHandler<CommitEventCommand>
{
    public async Task Handle(CommitEventCommand request, CancellationToken cancellationToken)
    {
        Event? entity = await repository.GetAsync(request.EventId, cancellationToken);
        if(entity is null)
        {
            return;
        }

        entity.Commit(request.ReporterUserId, request.CommitTime);
        _ = await repository.UpdateAsync(entity, cancellationToken);
    }
}

