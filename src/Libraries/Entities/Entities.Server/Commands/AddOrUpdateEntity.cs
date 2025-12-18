namespace Vitarish.Entities.Server.Commands;

public record AddOrUpdateEntityCommand<TEntity, TDescriptor>(TEntity Entity) : IRequest<TEntity?>
    where TEntity : Entity
    where TDescriptor : EntityDescriptor;

public class AddOrUpdateEntityHandler<TEntity, TDescriptor>(IEntityRepository<TEntity, TDescriptor> repository)
    : IRequestHandler<AddOrUpdateEntityCommand<TEntity, TDescriptor>, TEntity?>
    where TEntity : Entity
    where TDescriptor : EntityDescriptor
{
    public Task<TEntity?> Handle(AddOrUpdateEntityCommand<TEntity, TDescriptor> request, CancellationToken cancellationToken) => repository.AddOrUpdateAsync(request.Entity, cancellationToken);
}
