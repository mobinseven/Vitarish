using System.Text.Json.Serialization;

namespace Vitarish.Contents.Contracts.Models;

[JsonConverter(typeof(JsonStringEnumConverter<ContentItemType>))]
public enum ContentItemType { Article, BlogPost, Announcement, Page, Guide }
[JsonConverter(typeof(JsonStringEnumConverter<ContentStatus>))]
public enum ContentStatus { Draft, Scheduled, Published, Archived }
public sealed record ContentBodyBlock(string Type, string? Text, Guid? AssetId);
public sealed record ContentItemResponse(Guid Id, ContentItemType Type, ContentStatus Status, string Slug,
    string CanonicalPath, string Title, string Excerpt, IReadOnlyList<ContentBodyBlock> Body, string Locale,
    DateTime? PublishedAt, DateTime CreatedAt, DateTime? UpdatedAt);
public sealed record ContentRevisionResponse(Guid Id, int Version, DateTime CreatedAt);
