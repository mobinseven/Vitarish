namespace Vitarish.Entities.Server.Queries;

public record GetEntityByIdQuery<TEntity, TDescriptor>(Guid Id) : IRequest<TEntity?>
    where TEntity : Entity
    where TDescriptor : EntityDescriptor;

public class GetEntityByIdHandler<TEntity, TDescriptor>(IEntityRepository<TEntity, TDescriptor> repository)
    : IRequestHandler<GetEntityByIdQuery<TEntity, TDescriptor>, TEntity?>
    where TEntity : Entity
    where TDescriptor : EntityDescriptor
{
    public Task<TEntity?> Handle(GetEntityByIdQuery<TEntity, TDescriptor> request, CancellationToken cancellationToken) => repository.GetAsync(request.Id, cancellationToken);
}
