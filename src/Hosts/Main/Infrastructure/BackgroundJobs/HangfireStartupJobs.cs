using Vitarish.Main.Infrastructure.BackgroundJobs.Calendar;
using Vitarish.Main.Infrastructure.BackgroundJobs.Communications;
using Vitarish.Main.Infrastructure.BackgroundJobs.Files;
using Vitarish.Main.Infrastructure.BackgroundJobs.Intelligence;
using Vitarish.Main.Infrastructure.BackgroundJobs.Reminders;
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
        HangfireSeaJobManager seaJobs = app.Services.GetRequiredService<HangfireSeaJobManager>();
        seaJobs.ScheduleProcessing();
        seaJobs.TriggerProcessing();

        HangfireSeaIndexingJobManager seaIndexingJobs = app.Services.GetRequiredService<HangfireSeaIndexingJobManager>();
        seaIndexingJobs.ScheduleProcessing();
        seaIndexingJobs.TriggerProcessing();

        HangfireRemindersJobManager reminderJobs = app.Services.GetRequiredService<HangfireRemindersJobManager>();
        reminderJobs.ScheduleProcessing();
        reminderJobs.TriggerProcessing();

        HangfireFilesJobManager filesJobs = app.Services.GetRequiredService<HangfireFilesJobManager>();
        filesJobs.ScheduleProcessing();
        filesJobs.TriggerProcessing();

        HangfireIntelligenceJobManager intelligenceJobs = app.Services.GetRequiredService<HangfireIntelligenceJobManager>();
        intelligenceJobs.ScheduleProcessing();
        //intelligenceJobs.TriggerProcessing();

        HangfireCommunicationsJobManager commJobs = app.Services.GetRequiredService<HangfireCommunicationsJobManager>();
        commJobs.ScheduleProcessing();
        commJobs.TriggerProcessing();

        HangfireCalendarJobManager calendarJobs = app.Services.GetRequiredService<HangfireCalendarJobManager>();
        calendarJobs.ScheduleProcessing();
    }
}
