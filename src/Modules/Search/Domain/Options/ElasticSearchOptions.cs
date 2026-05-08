namespace Vitarish.Search.Domain.Options;

/// <summary>
/// Configuration for the Elasticsearch connection.
/// Bind from the "ElasticSearch" section in <c>appsettings.json</c>.
/// </summary>
public class ElasticSearchOptions
{
    /// <summary>
    /// Elasticsearch node URL (e.g. "http://localhost:9200").
    /// </summary>
    public string Url { get; set; } = "http://localhost:9200";

    /// <summary>
    /// Optional username for basic authentication.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Optional password for basic authentication.
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Prefix prepended to every index name (e.g. "vitarish-").
    /// </summary>
    public string DefaultIndexPrefix { get; set; } = "vitarish-";

    /// <summary>
    /// When enabled, semantic retrieval is expected to use only Elasticsearch native inference endpoints.
    /// External embedding/inference providers are intentionally unsupported.
    /// </summary>
    public bool UseNativeInferenceOnly { get; set; } = true;

    /// <summary>
    /// Elasticsearch inference endpoint identifier used by semantic retrieval workflows.
    /// Leave null when semantic retrieval is not configured yet.
    /// </summary>
    public string? InferenceEndpointId { get; set; }
}
