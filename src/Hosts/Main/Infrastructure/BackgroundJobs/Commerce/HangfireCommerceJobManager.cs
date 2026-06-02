using Hangfire;
using Vitarish.Commerce.Application.Abstractions;

namespace Vitarish.Main.Infrastructure.BackgroundJobs.Commerce;

/// <summary>
/// Configures recurring Hangfire jobs for Commerce catalog and order synchronization.
/// </summary>
public class HangfireCommerceJobManager(
    IRecurringJobManager recurringJobManager) : ICommerceJobManager
{
    private const string CatalogSyncJobId = "commerce:catalog:sync";
    private const string OrderSyncJobId = "commerce:orders:sync";
    private const string CartCleanupJobId = "commerce:carts:cleanup";

    public void ScheduleCatalogSync() => recurringJobManager.AddOrUpdate<CommerceCatalogSyncProcessor>(
            CatalogSyncJobId,
            processor => processor.ProcessAsync(CancellationToken.None),
            Cron.MinuteInterval(15));

    public void TriggerCatalogSync() => recurringJobManager.Trigger(CatalogSyncJobId);

    public void ScheduleOrderSync() => recurringJobManager.AddOrUpdate<CommerceOrderSyncProcessor>(
            OrderSyncJobId,
            processor => processor.ProcessAsync(CancellationToken.None),
            Cron.MinuteInterval(5));

    public void TriggerOrderSync() => recurringJobManager.Trigger(OrderSyncJobId);

    public void ScheduleCartCleanup() => recurringJobManager.AddOrUpdate<CommerceCartCleanupProcessor>(
            CartCleanupJobId,
            processor => processor.ProcessAsync(CancellationToken.None),
            Cron.Daily(3));
}
