namespace Vitarish.Modules.Elasticsearch.Domain.Metadata;

/// <summary>
/// Core metadata that identifies a searchable document independently of infrastructure concerns.
/// </summary>
/// <param name="EntityType">Domain entity type name.</param>
/// <param name="TenantId">Tenant identifier associated with the entity.</param>
/// <param name="EntityId">Entity identifier.</param>
/// <param name="SchemaVersion">Document schema version.</param>
public sealed record SearchDocumentMetadata(
    string EntityType,
    string TenantId,
    string EntityId,
    int SchemaVersion);
