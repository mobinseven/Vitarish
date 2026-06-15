using Hangfire;

namespace Vitarish.Main.Infrastructure.BackgroundJobs.Prescriptions;

/// <summary>
/// Configures recurring Hangfire jobs for prescription retry processing and reference data sync.
/// </summary>
public sealed class HangfirePrescriptionJobManager(IRecurringJobManager recurringJobManager)
{
    private const string RetryJobId = "prescription:sync-pending:retry";
    private const string ReferenceSyncJobId = "prescription:reference-data:sync";

    /// <summary>
    /// Adds or updates the recurring job that retries SyncPending prescriptions.
    /// </summary>
    public void ScheduleRetry() => recurringJobManager.AddOrUpdate<PrescriptionRetryProcessor>(
            RetryJobId,
            processor => processor.ProcessAsync(CancellationToken.None),
            Cron.MinuteInterval(5));

    /// <summary>
    /// Triggers the retry job immediately.
    /// </summary>
    public void TriggerRetry() => recurringJobManager.Trigger(RetryJobId);


    /// <summary>
    /// Triggers a reference data sync immediately (used for manual admin actions).
    /// </summary>
    public void TriggerReferenceDataSync() => recurringJobManager.Trigger(ReferenceSyncJobId);
}
