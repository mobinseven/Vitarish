using Hangfire;
using Microsoft.Extensions.Logging;

namespace Vitarish.Sea.Infrastructure.BackgroundJobs;

[DisableConcurrentExecution(timeoutInSeconds: 300)]
public sealed class ProjectObjectRefreshProcessor(
    IEnumerable<IProjectObjectMaterializedViewRefresher> refreshers,
    ILogger<ProjectObjectRefreshProcessor> logger)
{
    public async Task ProcessAsync(ProjectObjectRefreshRequest request, CancellationToken cancellationToken = default)
    {
        IProjectObjectMaterializedViewRefresher? refresher = refreshers.FirstOrDefault(item => string.Equals(item.ObjectType, request.ObjectType, StringComparison.Ordinal));
        if (refresher is null)
        {
            logger.LogWarning("No materialized-view refresher is registered for project object type {ObjectType}.", request.ObjectType);
            return;
        }

        await refresher.RefreshAsync(request.TenantId, cancellationToken);
    }

    public async Task ReconcileAsync(CancellationToken cancellationToken = default)
    {
        foreach (IProjectObjectMaterializedViewRefresher refresher in refreshers)
        {
            await refresher.RefreshAsync(string.Empty, cancellationToken);
        }
    }
}
