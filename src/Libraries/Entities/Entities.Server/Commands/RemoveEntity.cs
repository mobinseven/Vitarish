namespace Vitarish.Entities.Server.Commands;

public record RemoveEntityCommand<TEntity, TDescriptor>(TEntity Entity) : IRequest
    where TEntity : Entity
    where TDescriptor : EntityDescriptor;

public class RemoveEntityHandler<TEntity, TDescriptor>(IEntityRepository<TEntity, TDescriptor> repository)
    : IRequestHandler<RemoveEntityCommand<TEntity, TDescriptor>>
    where TEntity : Entity
    where TDescriptor : EntityDescriptor
{
    public async Task Handle(RemoveEntityCommand<TEntity, TDescriptor> request, CancellationToken cancellationToken)
    {
        await repository.RemoveAsync(request.Entity, cancellationToken);
    }
}
