namespace Vitarish.Modules.Elasticsearch.Domain.Metadata;

/// <summary>
/// Supported field data types for searchable document metadata.
/// </summary>
public enum SearchFieldDataType
{
    Text,
    Keyword,
    Integer,
    Long,
    Float,
    Double,
    Decimal,
    Boolean,
    DateTime,
    Guid,
    Object,
    Collection
}
