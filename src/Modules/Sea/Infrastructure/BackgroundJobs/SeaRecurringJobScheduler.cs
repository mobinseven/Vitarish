using Vitarish.Modules.Base.Core.BackgroundJobs;

namespace Vitarish.Sea.Infrastructure.BackgroundJobs;

/// <summary>
/// Schedules Sea recurring jobs and startup triggers.
/// </summary>
public sealed class SeaRecurringJobScheduler(
    HangfireSeaJobManager seaJobManager,
    HangfireSeaIndexingJobManager seaIndexingJobManager,
    HangfireRemindersJobManager remindersJobManager,
    HangfireProjectObjectRefreshJobManager projectObjectRefreshJobManager) : IRecurringJobScheduler
{
    public void ScheduleJobs()
    {
        seaJobManager.ScheduleProcessing();
        seaJobManager.ScheduleIntegrityVerification();
        seaIndexingJobManager.ScheduleProcessing();
        remindersJobManager.ScheduleProcessing();
        projectObjectRefreshJobManager.ScheduleProcessing();
    }

    public void TriggerStartupJobs()
    {
        seaJobManager.TriggerProcessing();
        seaJobManager.TriggerIntegrityVerification();
        seaIndexingJobManager.TriggerProcessing();
        remindersJobManager.TriggerProcessing();
        projectObjectRefreshJobManager.TriggerProcessing();
    }
}
