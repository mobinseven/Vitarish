namespace Vitarish.Entities.Server.Caching;

public interface IEntityCacheInvalidationService
{
    void Invalidate(Entity entity, EntityState entityState);
}
