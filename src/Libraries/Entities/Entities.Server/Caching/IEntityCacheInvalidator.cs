namespace Vitarish.Entities.Server.Caching;

public interface IEntityCacheInvalidator
{
    bool CanHandle(Entity entity);
    void Invalidate(Entity entity, EntityState entityState);
}
