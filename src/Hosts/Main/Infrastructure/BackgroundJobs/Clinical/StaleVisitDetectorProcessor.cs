using MediatR;
using Microsoft.EntityFrameworkCore;
using Vitarish.Clinical.Application.Events;
using Vitarish.Clinical.Domain.Models;
using Vitarish.Clinical.Infrastructure.Data;
using Vitarish.Main.Infrastructure.BackgroundJobs.Base;

namespace Vitarish.Main.Infrastructure.BackgroundJobs.Clinical;

/// <summary>
/// Hourly scan for workflows in non-terminal states older than a configurable threshold.
/// Flags stale visits and publishes FhirCommitFailedEvent if a FHIR inconsistency is found.
/// </summary>
public class StaleVisitDetectorProcessor(
    TenantBackgroundExecutor tenantBackgroundExecutor,
    ILogger<StaleVisitDetectorProcessor> logger)
{
    private const string BackgroundResource = "hangfire:clinical:stale:detector";
    private static readonly TimeSpan DefaultStaleThreshold = TimeSpan.FromHours(24);

    /// <summary>
    /// Detects stale visits for all tenants. Intended to be invoked by Hangfire.
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
        ClinicalDbContext dbContext = provider.GetRequiredService<ClinicalDbContext>();
        IMediator mediator = provider.GetRequiredService<IMediator>();

        try
        {
            DateTimeOffset staleThreshold = DateTimeOffset.UtcNow - DefaultStaleThreshold;

            VisitWorkflowStatus[] terminalStatuses = [VisitWorkflowStatus.Completed, VisitWorkflowStatus.Cancelled];

            List<Guid> staleWorkflowIds = await dbContext.ClinicalWorkflows
                .AsNoTracking()
                .Where(w => !terminalStatuses.Contains(w.Status) && w.CreatedAt < staleThreshold)
                .Select(w => w.Id)
                .Take(50)
                .ToListAsync(shutdownToken);

            foreach (Guid workflowId in staleWorkflowIds)
            {
                await mediator.Publish(new FhirCommitFailedEvent(
                    workflowId,
                    "StaleVisitCheck",
                    "StaleVisit",
                    $"stale-detector-{workflowId:N}"),
                    shutdownToken);

                logger.LogWarning("Stale visit detected: {WorkflowId}", workflowId);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Stale visit detector failed for tenant.");
        }
    }
}
