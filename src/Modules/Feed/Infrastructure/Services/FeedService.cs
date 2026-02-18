using System.Text.Json;
using Vitarish.Modules.Base;
using Vitarish.Modules.Feed.Domain.Abstractions;
using Vitarish.Modules.Feed.Domain.Models;

namespace Vitarish.Modules.Feed.Infrastructure.Services;

/// <summary>
/// Service that aggregates feed items from multiple data sources.
/// Auto-discovers all registered IFeedDataSource implementations via DI.
/// </summary>
public class FeedService : IFeedService
{
    private readonly IEnumerable<IFeedDataSource> _dataSources;
    private readonly ITenantService _tenantService;

    public FeedService(
        IEnumerable<IFeedDataSource> dataSources,
        ITenantService tenantService)
    {
        _dataSources = dataSources;
        _tenantService = tenantService;
    }

    public IReadOnlyList<IFeedDataSource> GetAvailableSources()
    {
        return _dataSources
            .OrderBy(s => s.Priority)
            .ToList();
    }

    public async Task<IReadOnlyList<FeedItem>> GetFeedAsync(
        FeedQueryOptions options,
        CancellationToken cancellationToken = default)
    {
        var config = GetConfiguration();
        
        // Filter sources based on configuration and options
        var sourcesToQuery = _dataSources
            .Where(s => ShouldIncludeSource(s, config, options))
            .ToList();

        if (sourcesToQuery.Count == 0)
        {
            return [];
        }

        // Fetch from all sources in parallel
        var fetchTasks = sourcesToQuery
            .Select(source => FetchFromSourceAsync(source, options, cancellationToken))
            .ToList();

        var results = await Task.WhenAll(fetchTasks);

        // Aggregate and sort all items
        var allItems = results
            .SelectMany(items => items)
            .OrderByDescending(item => item.Timestamp)
            .Skip(options.Skip)
            .Take(options.Limit)
            .ToList();

        return allItems;
    }

    private async Task<IReadOnlyList<FeedItem>> FetchFromSourceAsync(
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

    private bool ShouldIncludeSource(
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
        if (_tenantService.Metadata?.TryGetValue(configKey, out var configValue) == true
            && configValue is string configJson
            && !string.IsNullOrEmpty(configJson))
        {
            try
            {
                var config = JsonSerializer.Deserialize<FeedConfiguration>(configJson);
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

        if (_tenantService.Metadata != null)
        {
            var configJson = JsonSerializer.Serialize(configuration);
            _tenantService.Metadata[configKey] = configJson;
            
            // Trigger metadata update event
            await Task.CompletedTask; // Placeholder for actual persistence logic
        }
    }
}
