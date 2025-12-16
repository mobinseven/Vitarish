namespace Vitarish.Entities.Server.Caching;

public class EntityCacheInvalidator<TEntity>(IEntityCache<TEntity> cache) : IEntityCacheInvalidator
    where TEntity : Entity
{
    public bool CanHandle(Entity entity)
    {
        return entity is TEntity;
    }

    public void Invalidate(Entity entity, EntityState entityState)
    {
        if(entity is TEntity tEntity)
        {
            switch(entityState)
            {
                case EntityState.Added:
                case EntityState.Modified:
                    if(entity is not null)
                    {
                        cache.Set(tEntity);
                    }
                    break;
                case EntityState.Deleted:
                    cache.Remove(tEntity.Id);
                    break;
            }
        }
    }
}
