using Hangfire;

namespace Vitarish.Main.Infrastructure.BackgroundJobs.Intelligence;

public class HangfireIntelligenceJobManager(IRecurringJobManager recurringJobManager)
{
    private const string JobId = "intelligence:selftests";

    /// <summary>
    /// Adds or updates the recurring job.
    /// </summary>
    public void ScheduleProcessing() => recurringJobManager.AddOrUpdate<IntelligenceProcessor>(
            JobId,
            processor => processor.ProcessSelfTests(),
            Cron.MinuteInterval(15));

    /// <summary>
    /// Triggers the job immediately.
    /// </summary>
    public void TriggerProcessing() => recurringJobManager.Trigger(JobId);
}
