using Hangfire;

namespace Vitarish.Sea.Infrastructure.BackgroundJobs;

/// <summary>
/// Configures recurring Hangfire jobs for processing Sea reminders.
/// </summary>
public class HangfireRemindersJobManager(IRecurringJobManager recurringJobManager)
{
    private const string ProcessRemindersJobId = "sea:reminders:process-due";

    /// <summary>
    /// Adds or updates the recurring job that processes due reminders.
    /// </summary>
    public void ScheduleProcessing() => recurringJobManager.AddOrUpdate<ReminderProcessor>(
            ProcessRemindersJobId,
            processor => processor.ProcessDueReminders(),
            Cron.MinuteInterval(1));

    /// <summary>
    /// Triggers the reminder processing job immediately.
    /// </summary>
    public void TriggerProcessing() => recurringJobManager.Trigger(ProcessRemindersJobId);
}
