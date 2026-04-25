namespace Vitarish.Search.Domain.Options;

/// <summary>
/// Configuration for the Elasticsearch connection.
/// Bind from the "ElasticSearch" section in <c>appsettings.json</c>.
/// </summary>
public class ElasticSearchOptions
{
    public const string SectionName = "ElasticSearch";

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
}
