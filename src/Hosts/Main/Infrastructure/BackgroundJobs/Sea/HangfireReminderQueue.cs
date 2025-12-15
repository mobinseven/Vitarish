using Hangfire;

namespace Vitarish.Main.Infrastructure.BackgroundJobs.Sea;

/// <summary>
/// Configures recurring Hangfire jobs for processing Sea reminders.
/// </summary>
public class HangfireReminderQueue(IRecurringJobManager recurringJobManager)
{
    private const string ProcessRemindersJobId = "sea:reminders:process-due";

    /// <summary>
    /// Adds or updates the recurring job that processes due reminders.
    /// </summary>
    public void ScheduleProcessing()
    {
        recurringJobManager.AddOrUpdate<ReminderProcessor>(
            ProcessRemindersJobId,
            processor => processor.ProcessDueReminders(JobCancellationToken.Null),
            Cron.MinuteInterval(10));
    }

    /// <summary>
    /// Triggers the reminder processing job immediately.
    /// </summary>
    public void TriggerProcessing() => recurringJobManager.Trigger(ProcessRemindersJobId);
}
