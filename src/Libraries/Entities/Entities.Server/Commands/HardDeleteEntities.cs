namespace Vitarish.Entities.Server.Commands;

public record HardDeleteEntitiesCommand<TEntity, TDescriptor>(IReadOnlyList<TEntity> Entities) : IRequest
    where TEntity : Entity
    where TDescriptor : EntityDescriptor;

public class HardDeleteEntitiesHandler<TEntity, TDescriptor>(IEntityRepository<TEntity, TDescriptor> repository)
    : IRequestHandler<HardDeleteEntitiesCommand<TEntity, TDescriptor>>
    where TEntity : Entity
    where TDescriptor : EntityDescriptor
{
    public async Task Handle(HardDeleteEntitiesCommand<TEntity, TDescriptor> request, CancellationToken cancellationToken) => await repository.HardDeleteRangeAsync(request.Entities, cancellationToken);
}
