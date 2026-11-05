using System.Collections.Immutable;

namespace Vitarish.Contents.Domain.Models;

public sealed record ContentFieldDefinition
{
    public string Key { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public ContentFieldKind Kind { get; init; }
    public bool Required { get; init; }
    public bool Localized { get; init; }
    public int? MaxLength { get; init; }
    public ImmutableArray<string>? AllowedTypeKeys { get; init; }
    public ContentFieldDefinition? Item { get; init; }
    public ImmutableArray<ContentFieldDefinition> Fields { get; init; } = [];
}
