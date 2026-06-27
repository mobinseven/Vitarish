using System.Text.Json;
using Vitarish.Feed.Domain.Abstractions;
using Vitarish.Feed.Domain.Models;
using Vitarish.Modules.Aggregator.Core;

namespace Vitarish.Feed.Infrastructure.Services;

/// <summary>
/// Service that aggregates feed items from multiple data sources.
/// Auto-discovers all registered IFeedDataSource implementations via DI.
/// </summary>
public class FeedService(
    IEnumerable<IFeedDataSource> dataSources,
    ITenantService tenantService) : IFeedService
{
    public IReadOnlyList<IFeedDataSource> GetAvailableSources() => [.. dataSources.OrderBy(s => s.Priority)];

    public async Task<IReadOnlyList<FeedItem>> GetFeedAsync(
        FeedQueryOptions options,
        CancellationToken cancellationToken = default)
    {
        FeedConfiguration config = GetConfiguration();

        // Filter sources based on configuration and options
        List<IFeedDataSource> sourcesToQuery = [.. dataSources.Where(s => ShouldIncludeSource(s, config, options))];

        if (sourcesToQuery.Count == 0)
        {
            return [];
        }

        // Fetch from all sources in parallel
        List<Task<IReadOnlyList<FeedItem>>> fetchTasks = [.. sourcesToQuery.Select(source => FetchFromSourceAsync(source, options, cancellationToken))];

        IReadOnlyList<FeedItem>[] results = await Task.WhenAll(fetchTasks);

        // Aggregate and sort all items
        List<FeedItem> allItems = [.. results
            .SelectMany(items => items)
            .OrderByDescending(item => item.Timestamp)
            .Skip(options.Skip)
            .Take(options.Limit)];

        return allItems;
    }

    private static async Task<IReadOnlyList<FeedItem>> FetchFromSourceAsync(
        IFeedDataSource source,
        FeedQueryOptions options,
        CancellationToken cancellationToken)
    {
        try
        {
            return await source.GetFeedItemsAsync(options, cancellationToken);
        }
        catch
        {
            // Log error but don't fail the entire feed if one source fails
            // Return empty list for this source
            return [];
        }
    }

    private static bool ShouldIncludeSource(
        IFeedDataSource source,
        FeedConfiguration config,
        FeedQueryOptions options)
    {
        // If specific sources are requested in options, use those
        if (options.SourceIds?.Count > 0)
        {
            return options.SourceIds.Contains(source.SourceId);
        }

        // Otherwise use configuration
        if (config.EnabledSourceIds.Count > 0)
        {
            return config.EnabledSourceIds.Contains(source.SourceId);
        }

        // Fall back to source's default
        return source.EnabledByDefault;
    }

    public FeedConfiguration GetConfiguration()
    {
        const string configKey = "feed.configuration";

        // Try to get from tenant metadata
        if (tenantService.Metadata?.TryGetValue(configKey, out object? configValue) == true
            && configValue is string configJson
            && !string.IsNullOrEmpty(configJson))
        {
            try
            {
                FeedConfiguration? config = JsonSerializer.Deserialize<FeedConfiguration>(configJson);
                if (config != null)
                {
                    return config;
                }
            }
            catch
            {
                // Fall through to default
            }
        }

        // Return default configuration
        return new FeedConfiguration();
    }

    public async Task UpdateConfigurationAsync(
        FeedConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        const string configKey = "feed.configuration";

        if (tenantService.Metadata != null)
        {
            string configJson = JsonSerializer.Serialize(configuration);
            tenantService.Metadata[configKey] = configJson;

            // Trigger metadata update event
            await Task.CompletedTask; // Placeholder for actual persistence logic
        }
    }
}
