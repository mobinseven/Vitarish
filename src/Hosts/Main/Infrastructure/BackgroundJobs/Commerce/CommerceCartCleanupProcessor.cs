using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Vitarish.Commerce.Domain.Enums;
using Vitarish.Commerce.Infrastructure.Data;
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

            List<Vitarish.Commerce.Domain.Entities.Cart> abandoned = await dbContext.Carts
                .Where(c => c.Status == CartStatus.Open && c.CreatedAt < cutoff)
                .ToListAsync(shutdownToken);

            foreach (Vitarish.Commerce.Domain.Entities.Cart cart in abandoned)
            {
                cart.Status = CartStatus.Abandoned;
            }

            _ = await dbContext.SaveChangesAsync(shutdownToken);
            logger.LogInformation("Commerce cart cleanup: marked {Count} carts as abandoned.", abandoned.Count);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Commerce cart cleanup failed.");
        }
    }
}
