using Vitarish.Main.Infrastructure.BackgroundJobs.Base;
using Vitarish.Search.Application.Abstractions;

namespace Vitarish.Main.Infrastructure.BackgroundJobs.Sea;

/// <summary>
/// Rebuilds Elasticsearch indexes for all Sea entity types (projects, procedures, events, attachments).
/// </summary>
public class SeaIndexingProcessor(
    TenantBackgroundExecutor tenantBackgroundExecutor,
    ILogger<SeaIndexingProcessor> logger)
{
    private const string BackgroundResource = "hangfire:sea:search:reindex";

    private static readonly string[] IndexNames =
    [
        "sea.projects",
        "sea.procedures",
        "sea.events",
        "sea.attachments"
    ];

    /// <summary>
    /// Rebuilds all Sea search indexes for every tenant. Intended to be invoked by Hangfire as a recurring job.
    /// </summary>
    public async Task ProcessIndexingAsync(CancellationToken cancellationToken = default) => await tenantBackgroundExecutor.ExecutePerTenantAsync(
            BackgroundResource,
            PerTenantAsync,
            cancellationToken);

    private async Task PerTenantAsync(IServiceProvider provider, CancellationToken shutdownToken)
    {
        ISearchService searchService = provider.GetRequiredService<ISearchService>();

        int succeeded = 0;
        int failed = 0;

        foreach (string indexName in IndexNames)
        {
            try
            {
                await searchService.RebuildIndexAsync(indexName, shutdownToken);
                succeeded++;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to rebuild search index '{IndexName}'.", indexName);
                failed++;
            }
        }

        logger.LogInformation(
            "Sea search re-index complete: {Succeeded} succeeded, {Failed} failed.",
            succeeded, failed);
    }
}
