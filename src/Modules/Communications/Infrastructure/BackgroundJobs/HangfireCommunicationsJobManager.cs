using Hangfire;

namespace Vitarish.Communications.Infrastructure.BackgroundJobs;

public class HangfireCommunicationsJobManager(IRecurringJobManager recurringJobManager)
{
    private const string JobId = "communications:selftests";

    /// <summary>
    /// Adds or updates the recurring job.
    /// </summary>
    public void ScheduleProcessing() => recurringJobManager.AddOrUpdate<MessageProcessor>(
            JobId,
            processor => processor.ProcessSelfTests(),
            Cron.HourInterval(2));

    /// <summary>
    /// Triggers the job immediately.
    /// </summary>
    public void TriggerProcessing() => recurringJobManager.Trigger(JobId);
}
