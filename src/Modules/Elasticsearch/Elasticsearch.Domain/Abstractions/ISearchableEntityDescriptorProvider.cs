using Vitarish.Modules.Elasticsearch.Domain.Metadata;

namespace Vitarish.Modules.Elasticsearch.Domain.Abstractions;

/// <summary>
/// Contract for providing search metadata descriptors for a domain entity type.
/// </summary>
public interface ISearchableEntityDescriptorProvider
{
    /// <summary>
    /// Gets the logical entity type represented by this descriptor provider.
    /// </summary>
    string EntityType { get; }

    /// <summary>
    /// Gets the search field descriptors exposed for the entity.
    /// </summary>
    IReadOnlyCollection<SearchableFieldDescriptor> Fields { get; }
}
