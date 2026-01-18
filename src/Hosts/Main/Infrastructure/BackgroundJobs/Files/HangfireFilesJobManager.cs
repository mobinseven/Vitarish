using Hangfire;

namespace Vitarish.Main.Infrastructure.BackgroundJobs.Files;

public class HangfireFilesJobManager(IRecurringJobManager recurringJobManager)
{
    private const string JobId = "files:selftests";

    /// <summary>
    /// Adds or updates the recurring job.
    /// </summary>
    public void ScheduleProcessing() => recurringJobManager.AddOrUpdate<FilesProcessor>(
            JobId,
            processor => processor.ProcessSelfTests(),
            Cron.MinuteInterval(15));

    /// <summary>
    /// Triggers the job immediately.
    /// </summary>
    public void TriggerProcessing() => recurringJobManager.Trigger(JobId);
}
