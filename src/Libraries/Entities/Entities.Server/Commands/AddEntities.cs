namespace Vitarish.Entities.Server.Commands;

public record AddEntitiesCommand<TEntity, TDescriptor>(IReadOnlyList<TEntity> Entities) : IRequest<IReadOnlyList<TEntity>>
    where TEntity : Entity
    where TDescriptor : EntityDescriptor;

public class AddEntitiesHandler<TEntity, TDescriptor>(IEntityRepository<TEntity, TDescriptor> repository)
    : IRequestHandler<AddEntitiesCommand<TEntity, TDescriptor>, IReadOnlyList<TEntity>>
    where TEntity : Entity
    where TDescriptor : EntityDescriptor
{
    public Task<IReadOnlyList<TEntity>> Handle(AddEntitiesCommand<TEntity, TDescriptor> request, CancellationToken cancellationToken) => repository.AddRangeAsync(request.Entities, cancellationToken);
}
