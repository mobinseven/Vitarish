namespace Vitarish.Entities.Server.Caching;

public interface IEntityCache<TEntity>
    where TEntity : Entity
{
    TEntity? Get(Guid id);
    Task<TEntity?> GetOrAddAsync(Guid id, Func<Task<TEntity?>> factory);
    void Set(TEntity entity);
    void Remove(Guid id);
}
