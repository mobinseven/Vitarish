using Vitarish.Modules.Base.BackgroundJobs;

namespace Vitarish.Commerce.Infrastructure.BackgroundJobs;

/// <summary>
/// Schedules Commerce recurring jobs.
/// </summary>
public sealed class CommerceRecurringJobScheduler(HangfireCommerceJobManager commerceJobManager) : IRecurringJobScheduler
{
    public void ScheduleJobs()
    {
        commerceJobManager.ScheduleCatalogSync();
        commerceJobManager.ScheduleOrderSync();
        commerceJobManager.ScheduleCartCleanup();
    }

    public void TriggerStartupJobs()
    {
    }
}
