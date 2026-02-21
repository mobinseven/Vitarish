namespace Vitarish.Modules.Elasticsearch.Application.Models;

public sealed class SearchableFieldDescriptor
{
    public required string Name { get; init; }

    public SearchableFieldType Type { get; init; } = SearchableFieldType.Keyword;

    public bool IsSearchable { get; init; } = true;

    public bool IsFilterable { get; init; } = true;

    public bool IsSortable { get; init; }
}

public enum SearchableFieldType
{
    Keyword,
    Text,
    Integer,
    Long,
    Double,
    Boolean,
    Date
}
