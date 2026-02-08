using Hangfire;

namespace Vitarish.Main.Infrastructure.BackgroundJobs.Sea;

/// <summary>
/// Configures recurring Hangfire job for migrating Attachments from URL-based to ID-based content references.
/// </summary>
public class HangfireAttachmentMigrationJobManager(IRecurringJobManager recurringJobManager)
{
    private const string MigrationJobId = "sea:attachments:migrate-url-to-id";

    /// <summary>
    /// Adds or updates the recurring job that processes attachment migration.
    /// Runs daily at 2 AM to migrate any remaining URL-based attachments.
    /// </summary>
    public void ScheduleProcessing() => recurringJobManager.AddOrUpdate<AttachmentMigrationProcessor>(
            MigrationJobId,
            processor => processor.ProcessMigration(CancellationToken.None),
            Cron.Daily(2));

    /// <summary>
    /// Triggers the attachment migration job immediately.
    /// </summary>
    public void TriggerProcessing() => recurringJobManager.Trigger(MigrationJobId);
}
