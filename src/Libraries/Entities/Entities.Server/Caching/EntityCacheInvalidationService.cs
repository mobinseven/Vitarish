namespace Vitarish.Entities.Server.Caching;

public sealed class EntityCacheInvalidationService(IEnumerable<IEntityCacheInvalidator> cacheInvalidators) : IEntityCacheInvalidationService
{
    public void Invalidate(Entity entity, EntityState entityState)
    {
        foreach (IEntityCacheInvalidator invalidator in cacheInvalidators)
        {
            if (invalidator.CanHandle(entity))
            {
                invalidator.Invalidate(entity, entityState);
            }
        }
    }
}
