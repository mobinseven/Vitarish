# Feed widget plugin configuration

`FeedWidget` resolves feed sources from tenant metadata key `feedSources` and renders matching `IFeedPlugin` implementations in order.

## Tenant metadata format

```json
{
  "feedSources": [
    "sea.projects.recent",
    "sea.attachments.recent"
  ]
}
```

This metadata is stored per tenant and is persisted through `ITenantService.Metadata` updates.

## Add a new feed source plugin

1. Implement `IFeedPlugin` and expose a unique `Id`.
2. Return a component from `Render(FeedPlugin context)` and use `context.Take`/`context.Metadata` as needed.
3. Register the plugin in DI as `AddScoped<IFeedPlugin, YourPlugin>()`.
4. Add your plugin id to tenant metadata `feedSources` to enable it for that tenant.
