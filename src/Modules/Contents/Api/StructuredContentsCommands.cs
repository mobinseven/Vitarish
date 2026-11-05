using Microsoft.AspNetCore.Http;
using System.Collections.Immutable;
using Vitarish.Contents.Application.Abstractions;
using Vitarish.Contents.Contracts.Models;
using Vitarish.Entities.Contracts.Models;
using D = Vitarish.Contents.Domain;

namespace Vitarish.Contents.Api;

internal static partial class StructuredContentsApi
{
    private static async Task<IResult> CreateSiteAsync(SiteRequest r, IContentAuthoring authoring, CancellationToken ct) =>
        ValidateSite(r) ?? Outcome(await authoring.CreateSiteAsync(r.Key, r.Name, r.DefaultLocale, r.Locales, ct));

    private static async Task<IResult> UpdateSiteAsync(Guid siteId, SiteRequest r, IContentAuthoring authoring, CancellationToken ct) =>
        ValidateSite(r) ?? Outcome(await authoring.UpdateSiteAsync(siteId, r.Key, r.Name, r.DefaultLocale, r.Locales, ct));

    private static async Task<IResult> CreateTypeAsync(ContentTypeRequest r, IContentAuthoring authoring, CancellationToken ct) =>
        ValidateType(r) ?? Outcome(await authoring.CreateContentTypeAsync(r.Key, r.Name, r.Description, r.Fields.Select(ToDomain).ToArray(), ct));

    private static async Task<IResult> UpdateSchemaAsync(Guid typeId, ContentSchemaRequest r, IContentAuthoring authoring, CancellationToken ct) =>
        ValidateSchema(r) ?? Outcome(await authoring.UpdateContentTypeSchemaAsync(typeId, r.Fields.Select(ToDomain).ToArray(), ct));

    private static async Task<IResult> CreateEntryAsync(EntryRequest r, IContentAuthoring authoring, CancellationToken ct) =>
        (r.SiteId == Guid.Empty || r.ContentTypeId == Guid.Empty ? Results.BadRequest() : null) ??
        ValidateEntry(r.Slug, r.CanonicalPath, r.Title, r.Excerpt, r.Fields) ??
        Outcome(await authoring.CreateEntryAsync(r.SiteId, r.ContentTypeId, r.Slug, r.CanonicalPath, r.Title, r.Excerpt, r.Fields, ct));

    private static async Task<IResult> SaveDraftAsync(Guid id, DraftRequest r, IContentAuthoring authoring, CancellationToken ct) =>
        ValidateEntry(r.Slug, r.CanonicalPath, r.Title, r.Excerpt, r.Fields) ??
        Outcome(await authoring.SaveDraftAsync(id, r.ExpectedVersion, r.Slug, r.CanonicalPath, r.Title, r.Excerpt, r.Fields, ct));

    private static async Task<IResult> PublishAsync(Guid id, RevisionActionRequest r, IContentAuthoring authoring, CancellationToken ct) =>
        r.RevisionId == Guid.Empty ? Results.BadRequest() : Outcome(await authoring.PublishAsync(id, r.RevisionId, r.ExpectedVersion, ct));
    private static async Task<IResult> UnpublishAsync(Guid id, EntryVersionRequest r, IContentAuthoring authoring, CancellationToken ct) =>
        Outcome(await authoring.UnpublishAsync(id, r.ExpectedVersion, ct));
    private static async Task<IResult> ArchiveAsync(Guid id, EntryVersionRequest r, IContentAuthoring authoring, CancellationToken ct) =>
        Outcome(await authoring.ArchiveAsync(id, r.ExpectedVersion, ct));
    private static async Task<IResult> RestoreAsync(Guid id, RevisionActionRequest r, IContentAuthoring authoring, CancellationToken ct) =>
        r.RevisionId == Guid.Empty ? Results.BadRequest() : Outcome(await authoring.RestoreRevisionAsync(id, r.RevisionId, r.ExpectedVersion, ct));

    private static D.Models.ContentFieldDefinition ToDomain(ContentFieldDefinition field) => new()
    {
        Key = field.Key, Label = field.Label, Kind = (D.Enums.ContentFieldKind)(int)field.Kind,
        Required = field.Required, Localized = field.Localized, MaxLength = field.MaxLength,
        AllowedTypeKeys = field.AllowedTypeKeys?.ToImmutableArray(), Item = field.Item is null ? null : ToDomain(field.Item),
        Fields = field.Fields?.Select(ToDomain).ToImmutableArray() ?? []
    };
}
