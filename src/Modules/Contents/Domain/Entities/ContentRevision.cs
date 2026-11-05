using System.Text.Json;

namespace Vitarish.Contents.Domain.Entities;

public class ContentRevision : Entity
{
    public Guid ContentId { get; init; }
    public int Version { get; init; }
    public string Snapshot { get; init; } = string.Empty;
    public JsonElement? Fields { get; init; }
    public string? Title { get; init; }
    public string? Slug { get; init; }
    public string? CanonicalPath { get; init; }
    public string? Excerpt { get; init; }
    public int? SchemaVersion { get; init; }
}
