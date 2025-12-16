namespace Vitarish.Entities.Server.Commands;

public record RemoveEntitiesCommand<TEntity, TDescriptor>(IReadOnlyList<TEntity> Entities) : IRequest
    where TEntity : Entity
    where TDescriptor : EntityDescriptor;

public class RemoveEntitiesHandler<TEntity, TDescriptor>(IEntityRepository<TEntity, TDescriptor> repository)
    : IRequestHandler<RemoveEntitiesCommand<TEntity, TDescriptor>>
    where TEntity : Entity
    where TDescriptor : EntityDescriptor
{
    public async Task Handle(RemoveEntitiesCommand<TEntity, TDescriptor> request, CancellationToken cancellationToken)
    {
        await repository.RemoveRangeAsync(request.Entities, cancellationToken);
    }
}
