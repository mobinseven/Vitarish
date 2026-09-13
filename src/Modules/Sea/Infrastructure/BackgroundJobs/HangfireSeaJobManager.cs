using Hangfire;

namespace Vitarish.Sea.Infrastructure.BackgroundJobs;

/// <summary>
/// Configures recurring Hangfire jobs for processing Sea reminders.
/// </summary>
public class HangfireSeaJobManager(IRecurringJobManager recurringJobManager)
{
    private const string MigrationJobId = "sea:attachments:migrate-url-to-id";
    private const string IntegrityVerificationJobId = "sea:attachments:integrity-verification";

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

    public void ScheduleIntegrityVerification() => recurringJobManager.AddOrUpdate<AttachmentIntegrityVerificationProcessor>(
        IntegrityVerificationJobId,
        processor => processor.ProcessAsync(CancellationToken.None),
        Cron.MinuteInterval(10));

    public void TriggerIntegrityVerification() => recurringJobManager.Trigger(IntegrityVerificationJobId);
}
