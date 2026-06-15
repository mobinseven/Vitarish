using Vitarish.Modules.Base.BackgroundJobs;

namespace Vitarish.Sea.Infrastructure.BackgroundJobs;

/// <summary>
/// Schedules Sea recurring jobs and startup triggers.
/// </summary>
public sealed class SeaRecurringJobScheduler(
    HangfireSeaJobManager seaJobManager,
    HangfireSeaIndexingJobManager seaIndexingJobManager,
    HangfireRemindersJobManager remindersJobManager) : IRecurringJobScheduler
{
    public void ScheduleJobs()
    {
        seaJobManager.ScheduleProcessing();
        seaIndexingJobManager.ScheduleProcessing();
        remindersJobManager.ScheduleProcessing();
    }

    public void TriggerStartupJobs()
    {
        seaJobManager.TriggerProcessing();
        seaIndexingJobManager.TriggerProcessing();
        remindersJobManager.TriggerProcessing();
    }
}
