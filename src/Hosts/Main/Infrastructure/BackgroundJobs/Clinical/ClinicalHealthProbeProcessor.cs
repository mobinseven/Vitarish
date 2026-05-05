using Vitarish.Clinical.Domain.Abstractions;
using Vitarish.Main.Infrastructure.BackgroundJobs.Base;

namespace Vitarish.Main.Infrastructure.BackgroundJobs.Clinical;

/// <summary>
/// Runs a FHIR health probe for every tenant and persists a ConnectorHealthSnapshot.
/// </summary>
public class ClinicalHealthProbeProcessor(
    TenantBackgroundExecutor tenantBackgroundExecutor,
    ILogger<ClinicalHealthProbeProcessor> logger)
{
    private const string BackgroundResource = "hangfire:clinical:health:probe";

    /// <summary>
    /// Probes the clinical FHIR connector for all tenants. Intended to be invoked by Hangfire as a recurring job.
    /// </summary>
    public async Task ProcessAsync(CancellationToken cancellationToken = default)
    {
        await tenantBackgroundExecutor.ExecutePerTenantAsync(
            BackgroundResource,
            PerTenantAsync,
            cancellationToken);
    }

    private async Task PerTenantAsync(IServiceProvider provider, CancellationToken shutdownToken)
    {
        IClinicalConnectorHealthService healthService =
            provider.GetRequiredService<IClinicalConnectorHealthService>();

        try
        {
            _ = await healthService.ProbeAsync(cancellationToken: shutdownToken);
            logger.LogDebug("Clinical health probe completed for tenant.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Clinical health probe failed for tenant.");
        }
    }
}
