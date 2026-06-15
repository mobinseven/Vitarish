using Vitarish.Modules.Base.BackgroundJobs;

namespace Vitarish.Intelligence.Infrastructure.BackgroundJobs;

/// <summary>
/// Schedules Intelligence recurring jobs.
/// </summary>
public sealed class IntelligenceRecurringJobScheduler(HangfireIntelligenceJobManager intelligenceJobManager) : IRecurringJobScheduler
{
    public void ScheduleJobs() => intelligenceJobManager.ScheduleProcessing();

    public void TriggerStartupJobs()
    {
    }
}
