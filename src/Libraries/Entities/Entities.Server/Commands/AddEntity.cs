namespace Vitarish.Entities.Server.Commands;

public record AddEntityCommand<TEntity, TDescriptor>(TEntity Entity) : IRequest<TEntity?>
    where TEntity : Entity
    where TDescriptor : EntityDescriptor;

public class AddEntityHandler<TEntity, TDescriptor>(IEntityRepository<TEntity, TDescriptor> repository)
    : IRequestHandler<AddEntityCommand<TEntity, TDescriptor>, TEntity?>
    where TEntity : Entity
    where TDescriptor : EntityDescriptor
{
    public Task<TEntity?> Handle(AddEntityCommand<TEntity, TDescriptor> request, CancellationToken cancellationToken)
    {
        return repository.AddAsync(request.Entity, cancellationToken);
    }
}
