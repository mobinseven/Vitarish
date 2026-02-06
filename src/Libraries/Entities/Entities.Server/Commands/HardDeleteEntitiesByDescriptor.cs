namespace Vitarish.Entities.Server.Commands;

public record HardDeleteEntitiesByDescriptorCommand<TEntity, TDescriptor>(TDescriptor Descriptor) : IRequest
    where TEntity : Entity
    where TDescriptor : EntityDescriptor;

public class HardDeleteEntitiesByDescriptorHandler<TEntity, TDescriptor>(IEntityRepository<TEntity, TDescriptor> repository)
    : IRequestHandler<HardDeleteEntitiesByDescriptorCommand<TEntity, TDescriptor>>
    where TEntity : Entity
    where TDescriptor : EntityDescriptor
{
    public async Task Handle(HardDeleteEntitiesByDescriptorCommand<TEntity, TDescriptor> request, CancellationToken cancellationToken) => await repository.HardDeleteAsync(request.Descriptor, cancellationToken);
}
