using Hangfire;

namespace Vitarish.Sea.Infrastructure.BackgroundJobs;

/// <summary>
/// Configures the recurring Hangfire job that periodically rebuilds Sea search indexes.
/// </summary>
public class HangfireSeaIndexingJobManager(IRecurringJobManager recurringJobManager)
{
    private const string JobId = "sea:search:reindex";

    /// <summary>
    /// Adds or updates the recurring Sea search indexing job.
    /// </summary>
    public void ScheduleProcessing() => recurringJobManager.AddOrUpdate<SeaIndexingProcessor>(
            JobId,
            processor => processor.ProcessIndexingAsync(CancellationToken.None),
            Cron.HourInterval(6));

    /// <summary>
    /// Triggers the Sea search indexing job immediately.
    /// </summary>
    public void TriggerProcessing() => recurringJobManager.Trigger(JobId);
}
