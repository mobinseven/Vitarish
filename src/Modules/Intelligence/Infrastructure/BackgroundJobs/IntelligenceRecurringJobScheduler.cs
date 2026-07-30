using Vitarish.Modules.Base.Core.BackgroundJobs;

namespace Vitarish.Intelligence.Infrastructure.BackgroundJobs;

/// <summary>
/// Schedules Intelligence recurring jobs.
/// </summary>
public sealed class IntelligenceRecurringJobScheduler(HangfireIntelligenceJobManager intelligenceJobManager) : IRecurringJobScheduler
{
    public void ScheduleJobs()
    {
        intelligenceJobManager.ScheduleProcessing();
        intelligenceJobManager.ScheduleConversationCleanup();
    }

    public void TriggerStartupJobs()
    {
    }
}
