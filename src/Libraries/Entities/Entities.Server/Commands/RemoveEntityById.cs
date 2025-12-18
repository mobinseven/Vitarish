namespace Vitarish.Entities.Server.Commands;

public record RemoveEntityByIdCommand<TEntity, TDescriptor>(Guid Id) : IRequest
    where TEntity : Entity
    where TDescriptor : EntityDescriptor;

public class RemoveEntityByIdHandler<TEntity, TDescriptor>(IEntityRepository<TEntity, TDescriptor> repository)
    : IRequestHandler<RemoveEntityByIdCommand<TEntity, TDescriptor>>
    where TEntity : Entity
    where TDescriptor : EntityDescriptor
{
    public async Task Handle(RemoveEntityByIdCommand<TEntity, TDescriptor> request, CancellationToken cancellationToken)
    {
        TEntity? entity = await repository.GetAsync(request.Id, cancellationToken);
        if (entity == null)
        {
            return;
        }
        await repository.RemoveAsync(entity, cancellationToken);
    }
}
