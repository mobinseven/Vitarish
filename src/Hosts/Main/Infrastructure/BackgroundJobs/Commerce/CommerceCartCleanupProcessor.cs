using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vitarish.Commerce.Domain.Entities;
using Vitarish.Commerce.Domain.Enums;
using Vitarish.Commerce.Infrastructure.Data;
using Vitarish.Entities.Server.Core;
using Vitarish.Main.Infrastructure.BackgroundJobs.Base;

namespace Vitarish.Main.Infrastructure.BackgroundJobs.Commerce;

/// <summary>
/// Soft-deletes abandoned carts older than 48 hours.
/// </summary>
public class CommerceCartCleanupProcessor(
    TenantBackgroundExecutor tenantBackgroundExecutor,
    ILogger<CommerceCartCleanupProcessor> logger)
{
    private const string BackgroundResource = "hangfire:commerce:carts:cleanup";

    public async Task ProcessAsync(CancellationToken cancellationToken = default)
    {
        await tenantBackgroundExecutor.ExecutePerTenantAsync(
            BackgroundResource,
            PerTenantAsync,
            cancellationToken);
    }

    private async Task PerTenantAsync(IServiceProvider provider, CancellationToken shutdownToken)
    {
        CommerceDbContext dbContext = provider.GetRequiredService<CommerceDbContext>();

        try
        {
            DateTimeOffset cutoff = DateTimeOffset.UtcNow.AddHours(-48);

            int count = await dbContext.Carts
                .Where(c => c.Status == CartStatus.Open && c.CreatedAt < cutoff)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(c => c.Status, CartStatus.Abandoned)
                        .SetProperty(c => c.RecoveryStatus, CartRecoveryStatus.ContactPending),
                    shutdownToken);

            if (count > 0)
            {
                AuditEntry entry = new()
                {
                    Action = "BulkUpdated",
                    ActorId = "system:hangfire:commerce-cart-cleanup",
                    EntityType = nameof(Cart),
                    EntityId = Guid.Empty,
                    SnapshotBefore = null,
                    SnapshotAfter = $"{{\"status\":\"{CartStatus.Abandoned}\",\"recoveryStatus\":\"{CartRecoveryStatus.ContactPending}\",\"count\":{count}}}",
                    OccurredAt = DateTimeOffset.UtcNow
                };

                _ = await dbContext.Set<AuditEntry>().AddAsync(entry, shutdownToken);
                _ = await dbContext.SaveChangesAsync(shutdownToken);
            }

            logger.LogInformation("Commerce cart cleanup: marked {Count} carts as abandoned.", count);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Commerce cart cleanup failed.");
        }
    }
}
