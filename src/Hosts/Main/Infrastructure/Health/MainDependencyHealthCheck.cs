using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;
using Vitarish.Audit.Server;

namespace Vitarish.Main.Infrastructure.Health;

public static class MainHealthContract
{
    public const string ReadinessPath = "/health/ready";
    public const string ReadinessCheckName = "main-dependencies";
    public const string ReadinessTag = "readiness";
}

public sealed class MainDependencyHealthCheck(
    IDbContextFactory<AuditDbContext> dbContextFactory,
    IConnectionMultiplexer redis) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using AuditDbContext dbContext = dbContextFactory.CreateDbContext();
            if (!await dbContext.Database.CanConnectAsync(cancellationToken).ConfigureAwait(false))
                return HealthCheckResult.Unhealthy();

            if (!redis.IsConnected)
                return HealthCheckResult.Unhealthy();

            _ = await redis.GetDatabase().PingAsync(CommandFlags.None).WaitAsync(cancellationToken).ConfigureAwait(false);
            return HealthCheckResult.Healthy();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return HealthCheckResult.Unhealthy();
        }
    }
}
