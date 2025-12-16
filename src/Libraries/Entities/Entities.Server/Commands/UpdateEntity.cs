namespace Vitarish.Entities.Server.Commands;

public record UpdateEntityCommand<TEntity, TDescriptor>(TEntity Entity) : IRequest<TEntity?>
    where TEntity : Entity
    where TDescriptor : EntityDescriptor;

public class UpdateEntityHandler<TEntity, TDescriptor>(IEntityRepository<TEntity, TDescriptor> repository)
    : IRequestHandler<UpdateEntityCommand<TEntity, TDescriptor>, TEntity?>
    where TEntity : Entity
    where TDescriptor : EntityDescriptor
{
    public Task<TEntity?> Handle(UpdateEntityCommand<TEntity, TDescriptor> request, CancellationToken cancellationToken)
    {
        return repository.UpdateAsync(request.Entity, cancellationToken);
    }
}
