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
        recurringJobManager.AddOrUpdate<FilesProcessor>(
            JobId,
            processor => processor.ProcessSelfTests(),
            Cron.MinuteInterval(15));
        recurringJobManager.AddOrUpdate<FileMigrator>(
            MigrationJobId,
            processor => processor.ProcessMigration(),
            Cron.MinuteInterval(15));
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
