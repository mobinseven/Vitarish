using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Vitarish.Calendar.Application.Abstractions;
using Vitarish.Main.Infrastructure.BackgroundJobs.Base;

namespace Vitarish.Main.Infrastructure.BackgroundJobs.Calendar;

/// <summary>
/// Runs ICalendarSyncService.SyncAllAccountsAsync for every tenant on a recurring interval.
/// </summary>
public class CalendarSyncProcessor(
    TenantBackgroundExecutor tenantBackgroundExecutor,
    ILogger<CalendarSyncProcessor> logger)
{
    private const string JobName = "hangfire:calendar:sync";

    /// <summary>
    /// Syncs all active calendar accounts for all tenants. Invoked by Hangfire as a recurring job.
    /// </summary>
    public async Task ProcessSyncAsync(CancellationToken cancellationToken = default)
    {
        await tenantBackgroundExecutor.ExecutePerTenantAsync(
            JobName,
            PerTenantAsync,
            cancellationToken);
    }

    private async Task PerTenantAsync(IServiceProvider provider, CancellationToken shutdownToken)
    {
        ICalendarSyncService syncService = provider.GetRequiredService<ICalendarSyncService>();

        try
        {
            await syncService.SyncAllAccountsAsync(shutdownToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Calendar sync failed for tenant.");
        }
    }
}
