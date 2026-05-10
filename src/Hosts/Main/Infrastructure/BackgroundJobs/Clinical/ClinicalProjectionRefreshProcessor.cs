using Vitarish.Clinical.Domain.Abstractions;
using Vitarish.Main.Infrastructure.BackgroundJobs.Base;

namespace Vitarish.Main.Infrastructure.BackgroundJobs.Clinical;

/// <summary>
/// Refreshes operational projections (nurse queue, provider worklist, incomplete visits) for all tenants every 5 minutes.
/// </summary>
public class ClinicalProjectionRefreshProcessor(
    TenantBackgroundExecutor tenantBackgroundExecutor,
    ILogger<ClinicalProjectionRefreshProcessor> logger)
{
    private const string BackgroundResource = "hangfire:clinical:projection:refresh";

    /// <summary>
    /// Refreshes clinical queue projections for all tenants. Intended to be invoked by Hangfire.
    /// </summary>
    public async Task ProcessAsync(CancellationToken cancellationToken = default)
    {
        await tenantBackgroundExecutor.ExecutePerTenantAsync(
            BackgroundResource,
            PerTenantAsync,
            cancellationToken);
    }

    private async Task PerTenantAsync(IServiceProvider provider, CancellationToken shutdownToken)
    {
        IProjectionBuilder projectionBuilder = provider.GetRequiredService<IProjectionBuilder>();

        try
        {
            await projectionBuilder.RefreshNurseQueueAsync(shutdownToken);
            await projectionBuilder.RefreshProviderWorklistAsync(shutdownToken);
            await projectionBuilder.RefreshIncompleteVisitsAsync(shutdownToken);
            logger.LogDebug("Clinical projection refresh completed for tenant.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Clinical projection refresh failed for tenant.");
        }
    }
}
