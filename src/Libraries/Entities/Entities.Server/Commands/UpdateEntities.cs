namespace Vitarish.Entities.Server.Commands;

public record UpdateEntitiesCommand<TEntity, TDescriptor>(IReadOnlyList<TEntity> Entities) : IRequest<IReadOnlyList<TEntity>>
    where TEntity : Entity
    where TDescriptor : EntityDescriptor;

public class UpdateEntitiesHandler<TEntity, TDescriptor>(IEntityRepository<TEntity, TDescriptor> repository)
    : IRequestHandler<UpdateEntitiesCommand<TEntity, TDescriptor>, IReadOnlyList<TEntity>>
    where TEntity : Entity
    where TDescriptor : EntityDescriptor
{
    public async Task<IReadOnlyList<TEntity>> Handle(UpdateEntitiesCommand<TEntity, TDescriptor> request, CancellationToken cancellationToken)
    {
        await repository.UpdateRangeAsync(request.Entities, cancellationToken);
        return request.Entities;
    }
}
