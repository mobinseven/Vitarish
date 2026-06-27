using Microsoft.Extensions.Logging;
using Vitarish.Modules.Aggregator.Core.BackgroundJobs;

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
        ILogger logger = app.Logger;

        foreach (IRecurringJobScheduler scheduler in schedulers)
        {
            try
            {
                scheduler.ScheduleJobs();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to schedule background jobs for {SchedulerType}", scheduler.GetType().Name);
            }
        }

        foreach (IRecurringJobScheduler scheduler in schedulers)
        {
            try
            {
                scheduler.TriggerStartupJobs();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to trigger startup jobs for {SchedulerType}", scheduler.GetType().Name);
            }
        }
    }
}
