using Vitarish.Main.Infrastructure.BackgroundJobs.Sea;

namespace Vitarish.Main.Infrastructure.BackgroundJobs;

/// <summary>
/// Configures Hangfire jobs that need to be scheduled or triggered at application startup.
/// </summary>
public static class HangfireStartupJobs
{
    /// <summary>
    /// Registers and triggers Hangfire recurring jobs that should start processing immediately.
    /// </summary>
    public static void ScheduleSelfTriggeringJobs(this WebApplication app)
    {
        HangfireReminderQueue reminderQueue = app.Services.GetRequiredService<HangfireReminderQueue>();

        reminderQueue.ScheduleProcessing();
        reminderQueue.TriggerProcessing();
    }
}
