using Vitarish.Modules.Base.BackgroundJobs;

namespace Vitarish.Main.Infrastructure.BackgroundJobs;

/// <summary>
/// Configures host startup scheduling for module-owned recurring background jobs.
/// </summary>
public static class HangfireJobs
{
    /// <summary>
    /// Registers recurring jobs through module schedulers and triggers startup jobs separately.
    /// </summary>
    public static void ScheduleJobs(this WebApplication app)
    {
        IEnumerable<IRecurringJobScheduler> schedulers = app.Services.GetServices<IRecurringJobScheduler>();

        foreach (IRecurringJobScheduler scheduler in schedulers)
        {
            scheduler.ScheduleJobs();
        }

        foreach (IRecurringJobScheduler scheduler in schedulers)
        {
            scheduler.TriggerStartupJobs();
        }
    }
}
