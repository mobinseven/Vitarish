namespace Vitarish.Modules.Elasticsearch.Domain.Metadata;

/// <summary>
/// Describes a searchable field in a domain entity projection.
/// </summary>
/// <param name="FieldName">Logical field name in the search document.</param>
/// <param name="SourcePath">Path to the source value in the entity model.</param>
/// <param name="DataType">Declared semantic data type.</param>
/// <param name="IsFullText">Whether the field should support full-text search behavior.</param>
/// <param name="IsFilterable">Whether the field should support exact-match filtering.</param>
/// <param name="IsSortable">Whether the field should support sorting operations.</param>
/// <param name="Analyzer">Optional analyzer identifier.</param>
public sealed record SearchableFieldDescriptor(
    string FieldName,
    string SourcePath,
    SearchFieldDataType DataType,
    bool IsFullText,
    bool IsFilterable,
    bool IsSortable,
    string? Analyzer = null);
