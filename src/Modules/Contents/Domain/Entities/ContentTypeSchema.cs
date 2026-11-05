using System.Collections.Immutable;

namespace Vitarish.Contents.Domain.Entities;

public class ContentTypeSchema : Entity
{
    public Guid ContentTypeId { get; init; }
    public int Version { get; init; }
    public ImmutableArray<ContentFieldDefinition> Fields { get; init; } = [];
}
