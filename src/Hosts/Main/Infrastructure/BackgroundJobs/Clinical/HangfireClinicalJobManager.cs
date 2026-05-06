using Hangfire;

namespace Vitarish.Main.Infrastructure.BackgroundJobs.Clinical;

/// <summary>
/// Configures recurring Hangfire jobs for the Clinical FHIR connector.
/// </summary>
public class HangfireClinicalJobManager(IRecurringJobManager recurringJobManager)
{
    private const string HealthProbeJobId = "clinical:health:probe";
    private const string ProjectionRefreshJobId = "clinical:projection:refresh";
    private const string StaleVisitDetectorJobId = "clinical:stale:detector";

    /// <summary>
    /// Adds or updates the recurring health probe job (runs every 15 minutes).
    /// </summary>
    public void ScheduleProcessing()
    {
        recurringJobManager.AddOrUpdate<ClinicalHealthProbeProcessor>(
            HealthProbeJobId,
            processor => processor.ProcessAsync(CancellationToken.None),
            "*/15 * * * *");

        recurringJobManager.AddOrUpdate<ClinicalProjectionRefreshProcessor>(
            ProjectionRefreshJobId,
            processor => processor.ProcessAsync(CancellationToken.None),
            "*/5 * * * *");

        recurringJobManager.AddOrUpdate<StaleVisitDetectorProcessor>(
            StaleVisitDetectorJobId,
            processor => processor.ProcessAsync(CancellationToken.None),
            "0 * * * *");
    }

    /// <summary>
    /// Triggers the health probe job immediately.
    /// </summary>
    public void TriggerProcessing()
    {
        recurringJobManager.Trigger(HealthProbeJobId);
    }
}
