namespace Vitarish.Entities.Server.HealthChecks;
/// <summary>
/// Health check for entity cache operations.
/// Verifies that cache can perform basic operations.
/// </summary>
public class EntityCacheHealthCheck<TEntity>(IEntityCache<TEntity> cache) : IHealthCheck
    where TEntity : Entity, new()
{
    // Use a deterministic, well-known GUID for the test entity ID
    private static readonly Guid HealthCheckTestEntityId = Guid.Parse("00000000-0000-0000-0000-00000000DEAD");

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        TEntity testEntity = new()
        { Id = HealthCheckTestEntityId };

        try
        {
            // Test write operation
            cache.Set(testEntity);

            // Test read operation
            TEntity? retrieved = cache.Get(testEntity.Id);

            return retrieved?.Id == testEntity.Id
                ? HealthCheckResult.Healthy("Entity cache is operational")
                : HealthCheckResult.Degraded("Entity cache read returned unexpected result");
        }
        catch(Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                "Entity cache is not responding",
                ex);
        }
        finally
        {
            // Always cleanup test entity to prevent cache pollution
            try
            {
                cache.Remove(testEntity.Id);
            }
            catch(Exception ex)
            {
                // Ignore cleanup failures - health check already has the result
                Console.Error.WriteLine($"EntityCacheHealthCheck cleanup failed: {ex}");
            }
        }
    }
}
