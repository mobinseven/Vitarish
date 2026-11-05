using System.Text.Json;
using Vitarish.Contents.Domain.Entities;
using Vitarish.Contents.Domain.Enums;
using Vitarish.Contents.Domain.Models;

namespace Vitarish.Contents.Application.Models;

public sealed record ContentAuthoringOutcome(
    Guid Id,
    int? Version = null,
    Guid? RevisionId = null,
    ContentStatus? Status = null);

public sealed record SiteReadModel(
    Guid Id,
    string Key,
    string Name,
    string DefaultLocale,
    IReadOnlyList<string> Locales);

public sealed record ContentTypeReadModel(
    Guid Id,
    string Key,
    string Name,
    string? Description,
    int CurrentSchemaVersion,
    IReadOnlyList<ContentFieldDefinition> Fields);

public sealed record ContentEntryReadModel(
    Guid Id,
    Guid SiteId,
    Guid ContentTypeId,
    int SchemaVersion,
    int Version,
    string Slug,
    string CanonicalPath,
    string Title,
    string Excerpt,
    JsonElement Fields,
    Guid? WorkingRevisionId,
    Guid? PublishedRevisionId,
    ContentStatus Status,
    DateTime? PublishedAt);

public sealed record ContentRevisionReadModel(
    Guid Id,
    Guid ContentId,
    int Version,
    string Title,
    string Slug,
    string CanonicalPath,
    string Excerpt,
    JsonElement Fields,
    int SchemaVersion,
    string? CreatedBy,
    DateTime CreatedAt);

public sealed record PublishedContentEntryReadModel(
    Guid Id,
    Guid SiteId,
    Guid ContentTypeId,
    string TypeKey,
    int SchemaVersion,
    int RevisionVersion,
    string Slug,
    string CanonicalPath,
    string Title,
    string Excerpt,
    JsonElement Fields,
    DateTime PublishedAt);

public sealed record PublishedContentSnapshot(Site Site, ContentItem ContentItem, ContentRevision Revision);

public enum ContentPublicationOperation
{
    Index,
    Remove
}
