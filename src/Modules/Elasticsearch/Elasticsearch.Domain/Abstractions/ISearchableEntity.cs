using Vitarish.Modules.Elasticsearch.Domain.Metadata;

namespace Vitarish.Modules.Elasticsearch.Domain.Abstractions;

/// <summary>
/// Optional marker contract for entities that can expose searchable document metadata.
/// </summary>
public interface ISearchableEntity
{
    /// <summary>
    /// Returns identifying metadata for this entity when projected as a search document.
    /// </summary>
    SearchDocumentMetadata GetSearchDocumentMetadata();
}
