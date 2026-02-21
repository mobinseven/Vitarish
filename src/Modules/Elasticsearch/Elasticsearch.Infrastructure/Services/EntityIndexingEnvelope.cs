namespace Vitarish.Modules.Elasticsearch.Infrastructure.Services;

internal sealed record EntityIndexingEnvelope(
    string TenantId,
    string EntityType,
    string EntityId,
    DateTimeOffset OccurredAt,
    IReadOnlyDictionary<string, object?>? Snapshot,
    IReadOnlyDictionary<string, (object? Old, object? New)>? Changes);
