using Hangfire;

namespace Vitarish.Intelligence.Infrastructure.BackgroundJobs;

public class HangfireIntelligenceJobManager(IRecurringJobManager recurringJobManager)
{
    private const string JobId = "intelligence:selftests";

    /// <summary>
    /// Adds or updates the recurring job.
    /// </summary>
    public void ScheduleProcessing() => recurringJobManager.AddOrUpdate<IntelligenceProcessor>(
            JobId,
            processor => processor.ProcessSelfTests(),
            Cron.HourInterval(2));

    /// <summary>
    /// Triggers the job immediately.
    /// </summary>
    public void TriggerProcessing() => recurringJobManager.Trigger(JobId);
}
