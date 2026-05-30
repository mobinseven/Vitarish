using Hangfire;

namespace Vitarish.Main.Infrastructure.BackgroundJobs.Prescriptions;

/// <summary>
/// Configures recurring Hangfire jobs for prescription retry processing.
/// </summary>
public sealed class HangfirePrescriptionJobManager(IRecurringJobManager recurringJobManager)
{
    private const string RetryJobId = "prescription:sync-pending:retry";

    /// <summary>
    /// Adds or updates the recurring job that retries SyncPending prescriptions.
    /// </summary>
    public void ScheduleRetry()
    {
        recurringJobManager.AddOrUpdate<PrescriptionRetryProcessor>(
            RetryJobId,
            processor => processor.ProcessAsync(CancellationToken.None),
            Cron.MinuteInterval(5));
    }

    /// <summary>
    /// Triggers the retry job immediately.
    /// </summary>
    public void TriggerRetry()
    {
        recurringJobManager.Trigger(RetryJobId);
    }
}
