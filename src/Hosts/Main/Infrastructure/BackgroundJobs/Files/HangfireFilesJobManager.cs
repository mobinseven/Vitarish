using Hangfire;

namespace Vitarish.Main.Infrastructure.BackgroundJobs.Files;

public class HangfireFilesJobManager(IRecurringJobManager recurringJobManager)
{
    private const string JobId = "files:selftests";
    private const string MigrationJobId = "files:migration";

    /// <summary>
    /// Adds or updates the recurring job.
    /// </summary>
    public void ScheduleProcessing()
    {
        recurringJobManager.AddOrUpdate<FilesSelfTestsProcessor>(
            JobId,
            processor => processor.ProcessSelfTests(),
            Cron.MinuteInterval(1));
    }

    /// <summary>
    /// Triggers the job immediately.
    /// </summary>
    public void TriggerProcessing()
    {
        recurringJobManager.Trigger(JobId);
        recurringJobManager.Trigger(MigrationJobId);
    }
}
