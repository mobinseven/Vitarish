using Vitarish.Modules.Base.BackgroundJobs;

namespace Vitarish.Medical.Infrastructure.BackgroundJobs;

/// <summary>
/// Schedules Medical recurring jobs.
/// </summary>
public sealed class MedicalRecurringJobScheduler(HangfirePrescriptionJobManager prescriptionJobManager) : IRecurringJobScheduler
{
    public void ScheduleJobs() => prescriptionJobManager.ScheduleRetry();

    public void TriggerStartupJobs()
    {
    }
}
