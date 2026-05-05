using Hangfire;

namespace Vitarish.Main.Infrastructure.BackgroundJobs.Clinical;

/// <summary>
/// Configures recurring Hangfire jobs for the Clinical FHIR connector.
/// </summary>
public class HangfireClinicalJobManager(IRecurringJobManager recurringJobManager)
{
    private const string HealthProbeJobId = "clinical:health:probe";

    /// <summary>
    /// Adds or updates the recurring health probe job (runs every 15 minutes).
    /// </summary>
    public void ScheduleProcessing()
    {
        recurringJobManager.AddOrUpdate<ClinicalHealthProbeProcessor>(
            HealthProbeJobId,
            processor => processor.ProcessAsync(CancellationToken.None),
            "*/15 * * * *");
    }

    /// <summary>
    /// Triggers the health probe job immediately.
    /// </summary>
    public void TriggerProcessing()
    {
        recurringJobManager.Trigger(HealthProbeJobId);
    }
}
