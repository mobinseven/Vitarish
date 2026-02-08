using Vitarish.Main.Infrastructure.BackgroundJobs.Communications;
using Vitarish.Main.Infrastructure.BackgroundJobs.Files;
using Vitarish.Main.Infrastructure.BackgroundJobs.Intelligence;
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
        HangfireReminderJobManager reminderJobs = app.Services.GetRequiredService<HangfireReminderJobManager>();
        reminderJobs.ScheduleProcessing();
        reminderJobs.TriggerProcessing();

        HangfireFilesJobManager filesJobs = app.Services.GetRequiredService<HangfireFilesJobManager>();
        filesJobs.ScheduleProcessing();
        filesJobs.TriggerProcessing();

        HangfireIntelligenceJobManager intelligenceJobs = app.Services.GetRequiredService<HangfireIntelligenceJobManager>();
        intelligenceJobs.ScheduleProcessing();
        intelligenceJobs.TriggerProcessing();

        _ = app.Services.GetRequiredService<HangfireCommunicationsJobManager>();
        intelligenceJobs.ScheduleProcessing();
        intelligenceJobs.TriggerProcessing();

        HangfireAttachmentMigrationJobManager migrationJobs = app.Services.GetRequiredService<HangfireAttachmentMigrationJobManager>();
        migrationJobs.ScheduleProcessing();
        migrationJobs.TriggerProcessing();
    }
}
