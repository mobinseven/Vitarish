using Vitarish.Modules.Base.BackgroundJobs;

namespace Vitarish.Communications.Infrastructure.BackgroundJobs;

/// <summary>
/// Schedules Communications recurring jobs and startup triggers.
/// </summary>
public sealed class CommunicationsRecurringJobScheduler(
    HangfireRemindersJobManager remindersJobManager,
    HangfireCommunicationsJobManager communicationsJobManager) : IRecurringJobScheduler
{
    public void ScheduleJobs()
    {
        remindersJobManager.ScheduleProcessing();
        communicationsJobManager.ScheduleProcessing();
    }

    public void TriggerStartupJobs()
    {
        remindersJobManager.TriggerProcessing();
        communicationsJobManager.TriggerProcessing();
    }
}
