using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Vitarish.Commerce.Application.Abstractions;
using Vitarish.Modules.Base.BackgroundJobs;

namespace Vitarish.Commerce.Infrastructure.BackgroundJobs;

/// <summary>
/// Pulls orders from MedusaJS and upserts them in the local CommerceDbContext.
/// </summary>
public class CommerceOrderSyncProcessor(
    TenantBackgroundExecutor tenantBackgroundExecutor,
    ILogger<CommerceOrderSyncProcessor> logger)
{
    private const string BackgroundResource = "hangfire:commerce:orders:sync";

    public async Task ProcessAsync(CancellationToken cancellationToken = default) => await tenantBackgroundExecutor.ExecutePerTenantAsync(
            BackgroundResource,
            PerTenantAsync,
            cancellationToken);

    private async Task PerTenantAsync(IServiceProvider provider, CancellationToken shutdownToken)
    {
        ICommerceSyncService syncService = provider.GetRequiredService<ICommerceSyncService>();

        try
        {
            await syncService.SyncOrdersAsync(shutdownToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Commerce order sync processor encountered an unexpected error.");
        }
    }
}
