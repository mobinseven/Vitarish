namespace Vitarish.Modules.Elasticsearch.Application.Models;

public class IndexDocumentRequest
{
    public required string EntityType { get; init; }

    public required string EntityId { get; init; }

    public string? TenantId { get; init; }

    public required object Payload { get; init; }

    public long? Version { get; init; }

    public DateTimeOffset? Timestamp { get; init; }
}
