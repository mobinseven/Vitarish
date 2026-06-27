namespace Vitarish.Modules.Aggregator.Core.BackgroundJobs;

/// <summary>
/// Schedules module-owned recurring background jobs through a host-neutral convention.
/// </summary>
public interface IRecurringJobScheduler
{
    /// <summary>
    /// Registers or updates recurring jobs owned by the implementing module.
    /// </summary>
    void ScheduleJobs();

    /// <summary>
    /// Triggers startup jobs that should run immediately after recurring registration.
    /// </summary>
    void TriggerStartupJobs();
}
