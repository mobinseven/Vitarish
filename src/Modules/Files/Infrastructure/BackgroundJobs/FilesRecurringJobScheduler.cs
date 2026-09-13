using Vitarish.Modules.Base.Core.BackgroundJobs;

namespace Vitarish.Files.Infrastructure.BackgroundJobs;

/// <summary>
/// Schedules Files recurring jobs and startup triggers.
/// </summary>
public sealed class FilesRecurringJobScheduler(HangfireFilesJobManager filesJobManager) : IRecurringJobScheduler
{
    public void ScheduleJobs()
    {
        filesJobManager.ScheduleProcessing();
        filesJobManager.ScheduleIntegrityBaseline();
    }

    public void TriggerStartupJobs()
    {
        filesJobManager.TriggerProcessing();
        filesJobManager.TriggerIntegrityBaseline();
    }
}
