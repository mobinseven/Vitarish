namespace Vitarish.Entities.Server.Repositories;

public interface IEntityRepository<TEntity, TDescriptor>
    where TEntity : Entity
    where TDescriptor : EntityDescriptor
{
    Task<TEntity?> AddAsync(TEntity entity, CancellationToken cancellationToken);
    Task<IReadOnlyList<TEntity>> AddRangeAsync(IReadOnlyList<TEntity> entities, CancellationToken cancellationToken);
    Task<TEntity?> AddOrUpdateAsync(TEntity entity, CancellationToken cancellationToken);
    Task<int> CountAsync(TDescriptor descriptor, CancellationToken cancellationToken);
    Task<IReadOnlyList<TEntity>> GetAsync(TDescriptor descriptor, CancellationToken cancellationToken);
    Task<TEntity?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task RemoveAsync(TEntity entity, CancellationToken cancellationToken);
    Task RemoveAsync(TDescriptor entity, CancellationToken cancellationToken);
    Task RemoveRangeAsync(IReadOnlyList<TEntity> entities, CancellationToken cancellationToken);
    Task HardDeleteAsync(TEntity entity, CancellationToken cancellationToken);
    Task HardDeleteAsync(TDescriptor descriptor, CancellationToken cancellationToken);
    Task HardDeleteRangeAsync(IReadOnlyList<TEntity> entities, CancellationToken cancellationToken);
    Task<TEntity?> UpdateAsync(TEntity entity, CancellationToken cancellationToken);
    Task UpdateRangeAsync(IReadOnlyList<TEntity> entities, CancellationToken cancellationToken);
}