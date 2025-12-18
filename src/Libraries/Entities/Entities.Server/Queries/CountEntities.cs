namespace Vitarish.Entities.Server.Queries;

public record CountEntitiesQuery<TEntity, TDescriptor>(TDescriptor Descriptor) : IRequest<int>
    where TEntity : Entity
    where TDescriptor : EntityDescriptor;

public class CountEntitiesHandler<TEntity, TDescriptor>(IEntityRepository<TEntity, TDescriptor> repository)
    : IRequestHandler<CountEntitiesQuery<TEntity, TDescriptor>, int>
    where TEntity : Entity
    where TDescriptor : EntityDescriptor
{
    public Task<int> Handle(CountEntitiesQuery<TEntity, TDescriptor> request, CancellationToken cancellationToken) => repository.CountAsync(request.Descriptor, cancellationToken);
}
