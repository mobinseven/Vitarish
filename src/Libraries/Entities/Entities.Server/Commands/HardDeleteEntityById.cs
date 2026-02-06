namespace Vitarish.Entities.Server.Commands;

public record HardDeleteEntityByIdCommand<TEntity, TDescriptor>(Guid Id) : IRequest
    where TEntity : Entity
    where TDescriptor : EntityDescriptor;

public class HardDeleteEntityByIdHandler<TEntity, TDescriptor>(IEntityRepository<TEntity, TDescriptor> repository)
    : IRequestHandler<HardDeleteEntityByIdCommand<TEntity, TDescriptor>>
    where TEntity : Entity
    where TDescriptor : EntityDescriptor
{
    public async Task Handle(HardDeleteEntityByIdCommand<TEntity, TDescriptor> request, CancellationToken cancellationToken)
    {
        TEntity? entity = await repository.GetAsync(request.Id, cancellationToken);
        if (entity == null)
        {
            return;
        }
        await repository.HardDeleteAsync(entity, cancellationToken);
    }
}
