namespace Vitarish.Modules.Elasticsearch.Application.Models;

public abstract record EntityIndexingMessage(
    string TenantId,
    string EntityType,
    string EntityId,
    DateTimeOffset OccurredAt);

public sealed record EntityIndexed(
    string TenantId,
    string EntityType,
    string EntityId,
    object Payload,
    DateTimeOffset OccurredAt) : EntityIndexingMessage(TenantId, EntityType, EntityId, OccurredAt);

public sealed record EntityDeleted(
    string TenantId,
    string EntityType,
    string EntityId,
    DateTimeOffset OccurredAt) : EntityIndexingMessage(TenantId, EntityType, EntityId, OccurredAt);
