namespace Vitarish.Entities.Server.Commands;

public record RemoveEntitiesByDescriptorCommand<TEntity, TDescriptor>(TDescriptor Descriptor) : IRequest
    where TEntity : Entity
    where TDescriptor : EntityDescriptor;

public class RemoveEntitiesByDescriptorHandler<TEntity, TDescriptor>(IEntityRepository<TEntity, TDescriptor> repository)
    : IRequestHandler<RemoveEntitiesByDescriptorCommand<TEntity, TDescriptor>>
    where TEntity : Entity
    where TDescriptor : EntityDescriptor
{
    public async Task Handle(RemoveEntitiesByDescriptorCommand<TEntity, TDescriptor> request, CancellationToken cancellationToken)
    {
        await repository.RemoveAsync(request.Descriptor, cancellationToken);
    }
}
