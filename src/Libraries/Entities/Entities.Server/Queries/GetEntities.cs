namespace Vitarish.Entities.Server.Queries;

public record GetEntitiesQuery<TEntity, TDescriptor>(TDescriptor Descriptor) : IRequest<IReadOnlyList<TEntity>>
    where TEntity : Entity
    where TDescriptor : EntityDescriptor;

public class GetEntitiesHandler<TEntity, TDescriptor>(IEntityRepository<TEntity, TDescriptor> repository)
    : IRequestHandler<GetEntitiesQuery<TEntity, TDescriptor>, IReadOnlyList<TEntity>>
    where TEntity : Entity
    where TDescriptor : EntityDescriptor
{
    public Task<IReadOnlyList<TEntity>> Handle(GetEntitiesQuery<TEntity, TDescriptor> request, CancellationToken cancellationToken) => repository.GetAsync(request.Descriptor, cancellationToken);
}
