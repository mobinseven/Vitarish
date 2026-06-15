using Vitarish.Modules.Base.BackgroundJobs;

namespace Vitarish.Communications.Infrastructure.BackgroundJobs;

/// <summary>
/// Schedules Communications recurring jobs and startup triggers.
/// </summary>
public sealed class CommunicationsRecurringJobScheduler(
    HangfireCommunicationsJobManager communicationsJobManager) : IRecurringJobScheduler
{
    public void ScheduleJobs()
    {
        communicationsJobManager.ScheduleProcessing();
    }

    public void TriggerStartupJobs()
    {
        communicationsJobManager.TriggerProcessing();
    }
}
