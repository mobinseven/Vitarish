using Hangfire;

namespace Vitarish.Sea.Infrastructure.BackgroundJobs;

/// <summary>
/// Configures recurring Hangfire jobs for processing Sea reminders.
/// </summary>
public class HangfireSeaJobManager(IRecurringJobManager recurringJobManager)
{
    private const string MigrationJobId = "sea:attachments:migrate-url-to-id";

    /// <summary>
    /// Adds or updates the recurring job that processes due reminders.
    /// </summary>
    public void ScheduleProcessing() => recurringJobManager.AddOrUpdate<AttachmentMigrationProcessor>(
            MigrationJobId,
            processor => processor.ProcessMigration(CancellationToken.None),
            Cron.Daily(2));

    /// <summary>
    /// Triggers the reminder processing job immediately.
    /// </summary>
    public void TriggerProcessing() => recurringJobManager.Trigger(MigrationJobId);
}
