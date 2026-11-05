using System.Text.Json;
using System.Text.Json.Serialization;

namespace Vitarish.Contents.Contracts.Models;

[JsonConverter(typeof(JsonStringEnumConverter<ContentFieldKind>))]
public enum ContentFieldKind { Text, LongText, RichText, Number, Boolean, Date, Media, Link, Reference, List, Group }

public sealed record ContentFieldDefinition(
    string Key, string Label, [property: JsonRequired] ContentFieldKind Kind,
    [property: JsonRequired] bool Required, [property: JsonRequired] bool Localized,
    int? MaxLength, IReadOnlyList<string>? AllowedTypeKeys, ContentFieldDefinition? Item,
    IReadOnlyList<ContentFieldDefinition>? Fields);

public sealed record SiteRequest(string Key, string Name, string DefaultLocale, string[] Locales);
public sealed record ContentTypeRequest(string Key, string Name, string? Description, IReadOnlyList<ContentFieldDefinition> Fields);
public sealed record ContentSchemaRequest(IReadOnlyList<ContentFieldDefinition> Fields);
public sealed record EntryRequest(Guid SiteId, Guid ContentTypeId, string Slug, string CanonicalPath,
    string Title, string? Excerpt, JsonElement Fields);
public sealed record DraftRequest([property: JsonRequired] int ExpectedVersion, string Slug, string CanonicalPath, string Title,
    string? Excerpt, JsonElement Fields);
public sealed record EntryVersionRequest([property: JsonRequired] int ExpectedVersion);
public sealed record RevisionActionRequest([property: JsonRequired] Guid RevisionId, [property: JsonRequired] int ExpectedVersion);

public sealed record SiteResponse(Guid Id, string Key, string Name, string DefaultLocale, string[] Locales);
public sealed record ContentTypeResponse(Guid Id, string Key, string Name, string? Description, int CurrentSchemaVersion,
    IReadOnlyList<ContentFieldDefinition> Fields);
public sealed record EntryOutcome(Guid Id,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] int? Version,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] Guid? RevisionId,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] ContentStatus? Status);
public sealed record EntryResponse(Guid Id, Guid SiteId, Guid ContentTypeId, int SchemaVersion, string Slug,
    string CanonicalPath, string Title, string? Excerpt, JsonElement Fields, int Version, ContentStatus Status,
    Guid? WorkingRevisionId, Guid? PublishedRevisionId, DateTime? PublishedAt);
public sealed record RevisionResponse(Guid Id, int Version, int SchemaVersion, string Slug, string CanonicalPath,
    string Title, string? Excerpt, JsonElement Fields, string? CreatedBy, DateTime CreatedAt);
public sealed record PublishedEntryResponse(Guid Id, string TypeKey, string Slug, string CanonicalPath, string Title,
    string? Excerpt, JsonElement Fields, DateTime? PublishedAt);
