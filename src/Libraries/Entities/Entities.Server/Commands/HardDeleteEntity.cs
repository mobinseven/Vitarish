namespace Vitarish.Entities.Server.Commands;

public record HardDeleteEntityCommand<TEntity, TDescriptor>(TEntity Entity) : IRequest
    where TEntity : Entity
    where TDescriptor : EntityDescriptor;

public class HardDeleteEntityHandler<TEntity, TDescriptor>(IEntityRepository<TEntity, TDescriptor> repository)
    : IRequestHandler<HardDeleteEntityCommand<TEntity, TDescriptor>>
    where TEntity : Entity
    where TDescriptor : EntityDescriptor
{
    public async Task Handle(HardDeleteEntityCommand<TEntity, TDescriptor> request, CancellationToken cancellationToken) => await repository.HardDeleteAsync(request.Entity, cancellationToken);
}
