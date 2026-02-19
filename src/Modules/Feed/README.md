# Feed Module

A scalable Twitter/X-style feed infrastructure providing pluggable feed data sources and tenant-based configuration.

## Architecture

The Feed module follows the standard Domain-Driven Design (DDD) layering:

```
Feed/
├── Domain/           # Core abstractions and models
│   ├── Abstractions/ # IFeedDataSource, IFeedService
│   └── Models/       # FeedItem, FeedConfiguration, FeedQueryOptions
├── Application/      # CQRS queries
│   └── Queries/      # GetFeedQuery, GetFeedSourcesQuery, GetFeedConfigurationQuery
└── Infrastructure/   # Implementation
    ├── Services/     # FeedService
    └── Handlers/     # MediatR query handlers
```

## Core Concepts

### Feed Data Source (`IFeedDataSource`)

An interface that modules/estates implement to provide feed items. Each data source represents a specific content type (e.g., attachments, projects, events).

**Key Properties:**
- `SourceId`: Unique identifier (e.g., "sea.attachments")
- `SourceName`: Display name (e.g., "Sea Attachments")
- `Priority`: Display order (lower = higher priority)
- `EnabledByDefault`: Whether the source is enabled by default

**Key Method:**
```csharp
Task<IReadOnlyList<FeedItem>> GetFeedItemsAsync(
    FeedQueryOptions options,
    CancellationToken cancellationToken = default);
```

### Feed Item (`FeedItem`)

A unified model representing any type of feed content:

```csharp
public record FeedItem
{
    public Guid Id { get; init; }
    public string SourceId { get; init; }
    public string SourceName { get; init; }
    public string Title { get; init; }
    public string? Description { get; init; }
    public DateTimeOffset Timestamp { get; init; }
    public string ItemType { get; init; }
    public string? IconUrl { get; init; }
    public string? NavigationUrl { get; init; }
    public string? CreatedBy { get; init; }
    public IReadOnlyDictionary<string, object?> Metadata { get; init; }
}
```

### Feed Service (`IFeedService`)

The central service that:
1. Auto-discovers all registered feed data sources via DI
2. Aggregates items from multiple sources
3. Sorts and filters based on configuration
4. Manages tenant-specific feed configuration

## Implementation Guide

### Creating a Feed Data Source

1. **Implement `IFeedDataSource`:**

```csharp
public class MyModuleFeedSource : IFeedDataSource
{
    private readonly IMediator _mediator;

    public MyModuleFeedSource(IMediator mediator)
    {
        _mediator = mediator;
    }

    public string SourceId => "mymodule.items";
    public string SourceName => "My Module Items";
    public string Description => "Recent items from My Module";
    public int Priority => 10;
    public bool EnabledByDefault => true;

    public async Task<IReadOnlyList<FeedItem>> GetFeedItemsAsync(
        FeedQueryOptions options,
        CancellationToken cancellationToken = default)
    {
        // Query your module's data
        var items = await _mediator.Send(
            new GetRecentItemsQuery(options.Limit), 
            cancellationToken);
        
        // Convert to FeedItem
        return items.Select(CreateFeedItem).ToList();
    }

    private FeedItem CreateFeedItem(MyItem item)
    {
        return new FeedItem
        {
            Id = item.Id,
            SourceId = SourceId,
            SourceName = SourceName,
            Title = item.Name,
            Description = item.Description,
            Timestamp = item.UpdatedAt ?? item.CreatedAt,
            ItemType = "myitem",
            NavigationUrl = $"/mymodule/items/{item.Id}",
            CreatedBy = item.CreatedBy,
            Metadata = new Dictionary<string, object?>
            {
                ["customField"] = item.CustomData
            }
        };
    }
}
```

2. **Register in DI:**

```csharp
public static class MyModuleExtensions
{
    public static IServiceCollection AddMyModule(this IServiceCollection services)
    {
        // Register as IFeedDataSource for auto-discovery
        services.AddScoped<IFeedDataSource, MyModuleFeedSource>();
        
        return services;
    }
}
```

### Using the Feed Widget

Add the `FeedWidget` component to any Razor page:

```razor
@using Vitarish.Interfaces.Widgets.Components

<FeedWidget 
    Title="Activity Feed"
    PageSize="20"
    ShowRefreshButton="true"
    OnItemClicked="HandleItemClick" />

@code {
    private async Task HandleItemClick(FeedItem item)
    {
        // Navigate or show details
        NavigationManager.NavigateTo(item.NavigationUrl ?? "/");
    }
}
```

### Configuration

Feed configuration is stored in tenant metadata with key `"feed.configuration"`:

```json
{
  "enabledSourceIds": ["sea.attachments", "sea.projects"],
  "defaultPageSize": 50,
  "maxPageSize": 100,
  "sortOrder": "timestamp_desc",
  "autoRefresh": false,
  "autoRefreshIntervalSeconds": 60
}
```

## Examples

### Sea Estate Feed Sources

The Sea estate provides two feed sources:

1. **SeaAttachmentsFeedSource** (`sea.attachments`)
   - Shows recent file attachments
   - Priority: 10

2. **SeaProjectsFeedSource** (`sea.projects`)
   - Shows recently created/updated projects
   - Priority: 20

## API Reference

### Queries

**GetFeedQuery**
```csharp
public record GetFeedQuery(FeedQueryOptions Options) 
    : IRequest<IReadOnlyList<FeedItem>>;
```

**GetFeedSourcesQuery**
```csharp
public record GetFeedSourcesQuery 
    : IRequest<IReadOnlyList<IFeedDataSource>>;
```

**GetFeedConfigurationQuery**
```csharp
public record GetFeedConfigurationQuery 
    : IRequest<FeedConfiguration>;
```

### FeedQueryOptions

```csharp
public record FeedQueryOptions
{
    public int Limit { get; init; } = 50;
    public int Skip { get; init; } = 0;
    public IReadOnlyList<string>? SourceIds { get; init; }
    public IReadOnlyList<string>? ItemTypes { get; init; }
    public DateTimeOffset? After { get; init; }
    public DateTimeOffset? Before { get; init; }
    public Guid? TenantId { get; init; }
}
```

## Best Practices

1. **Performance**: Keep feed queries efficient. Use database indexes on timestamp columns.

2. **Error Handling**: Feed sources should catch exceptions and return empty lists rather than failing the entire feed.

3. **Pagination**: Use `Limit` and `Skip` for pagination. Don't fetch all items at once.

4. **Caching**: Consider caching recent items if your data source is expensive to query.

5. **Metadata**: Use the `Metadata` dictionary for custom fields that don't fit the standard model.

6. **Item Types**: Use consistent naming for `ItemType` to enable filtering (e.g., "attachment", "project", "event").

## Future Enhancements

- Real-time updates via SignalR
- Configurable sort orders (by type, by source, by custom fields)
- Feed item actions (like, comment, share)
- Advanced filtering UI
- Feed source enable/disable UI
- Per-user feed preferences
