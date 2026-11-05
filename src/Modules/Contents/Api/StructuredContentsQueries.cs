using Microsoft.AspNetCore.Http;
using Finbuckle.MultiTenant.Abstractions;
using Vitarish.Contents.Application;
using Vitarish.Contents.Application.Models;
using Vitarish.Contents.Contracts.Models;
using Vitarish.Entities.Contracts.Models;
using Vitarish.Requests.Application.Abstractions;
using D = Vitarish.Contents.Domain;

namespace Vitarish.Contents.Api;

internal static partial class StructuredContentsApi
{
    private static async Task<IResult> ListSitesAsync(ContentAuthoringQueries q, CancellationToken ct) =>
        Map(await q.ListSitesAsync(ct), items => items.Select(Site).ToArray());
    private static async Task<IResult> ListTypesAsync(ContentAuthoringQueries q, CancellationToken ct) =>
        Map(await q.ListContentTypesAsync(ct), items => items.Select(Type).ToArray());
    private static async Task<IResult> GetTypeAsync(Guid typeId, ContentAuthoringQueries q, CancellationToken ct) =>
        Map(await q.GetContentTypeAsync(typeId, ct), Type);
    private static async Task<IResult> ListEntriesAsync(Guid siteId, Guid? typeId, ContentStatus? status, int? skip, int? take, ContentAuthoringQueries q, CancellationToken ct)
    {
        if (siteId == Guid.Empty || !ValidPage(skip, take, 100) || status is not null && !Enum.IsDefined(status.Value)) return Results.BadRequest();
        return Map(await q.ListEntriesAsync(siteId, typeId, status is null ? null : (D.Enums.ContentStatus)(int)status.Value, skip ?? 0, take ?? 20, ct),
            items => items.Select(Entry).ToArray());
    }
    private static async Task<IResult> GetEntryAsync(Guid id, ContentAuthoringQueries q, CancellationToken ct) =>
        Map(await q.GetEntryAsync(id, ct), Entry);
    private static async Task<IResult> ListRevisionsAsync(Guid id, ContentAuthoringQueries q, CancellationToken ct) =>
        Map(await q.ListRevisionsAsync(id, ct), items => items.Select(Revision).ToArray());
    private static async Task<IResult> GetRevisionAsync(Guid id, Guid revisionId, ContentAuthoringQueries q, CancellationToken ct) =>
        Map(await q.GetRevisionAsync(id, revisionId, ct), Revision);

    private static async Task<IResult> GetPublicSiteAsync(string siteKey, HttpContext http, ContentDelivery q, IRequestContext request, IMultiTenantContextAccessor tenant, CancellationToken ct)
    {
        if (!HostMatches(request, tenant)) return Results.NotFound();
        if (!ValidKey(siteKey)) return Results.BadRequest();
        var result = await q.GetSiteAsync(siteKey, ct);
        return result.IsSuccess && result.Value is not null
            ? PublicContentResponse.Create(http, Site(result.Value), ContentsJsonSerializerContext.Default.SiteResponse)
            : Error(result.Errors);
    }
    private static async Task<IResult> GetPublicEntryAsync(string siteKey, string path, HttpContext http, ContentDelivery q, IRequestContext request, IMultiTenantContextAccessor tenant, CancellationToken ct)
    {
        if (!HostMatches(request, tenant)) return Results.NotFound();
        if (!ValidKey(siteKey) || string.IsNullOrWhiteSpace(path) || path.Length > 512) return Results.BadRequest();
        var result = await q.GetPublishedEntryByPathAsync(siteKey, path, ct);
        return result.IsSuccess && result.Value is not null
            ? PublicContentResponse.Create(http, Published(result.Value), ContentsJsonSerializerContext.Default.PublishedEntryResponse)
            : Error(result.Errors);
    }
    private static async Task<IResult> ListPublicEntriesAsync(string siteKey, string typeKey, int? skip, int? take, HttpContext http, ContentDelivery q, IRequestContext request, IMultiTenantContextAccessor tenant, CancellationToken ct)
    {
        if (!HostMatches(request, tenant)) return Results.NotFound();
        if (!ValidKey(siteKey) || !ValidKey(typeKey)) return Results.BadRequest();
        if (!ValidPage(skip, take, 50)) return Results.BadRequest();
        var result = await q.ListPublishedEntriesAsync(siteKey, typeKey, skip ?? 0, take ?? 20, ct);
        return result.IsSuccess && result.Value is not null
            ? PublicContentResponse.Create(http, result.Value.Select(Published).ToArray(), ContentsJsonSerializerContext.Default.PublishedEntryResponseArray)
            : Error(result.Errors);
    }

    private static bool ValidPage(int? skip, int? take, int max) => (skip is null or >= 0) && (take is null || take > 0 && take <= max);
    private static bool ValidKey(string key) => !string.IsNullOrWhiteSpace(key) && key.Length <= 256;
    private static bool HostMatches(IRequestContext request, IMultiTenantContextAccessor tenant) =>
        !string.IsNullOrWhiteSpace(tenant.MultiTenantContext?.TenantInfo?.Id) &&
        string.Equals(request.TenantId, tenant.MultiTenantContext.TenantInfo.Id, StringComparison.Ordinal);
    private static SiteResponse Site(SiteReadModel s) => new(s.Id, s.Key, s.Name, s.DefaultLocale, s.Locales.ToArray());
    private static ContentTypeResponse Type(ContentTypeReadModel t) => new(t.Id, t.Key, t.Name, t.Description, t.CurrentSchemaVersion, t.Fields.Select(Field).ToArray());
    private static ContentFieldDefinition Field(D.Models.ContentFieldDefinition f) => new(f.Key, f.Label, (ContentFieldKind)(int)f.Kind,
        f.Required, f.Localized, f.MaxLength, f.AllowedTypeKeys?.ToArray(), f.Item is null ? null : Field(f.Item), f.Fields.Select(Field).ToArray());
    private static EntryResponse Entry(ContentEntryReadModel e) => new(e.Id, e.SiteId, e.ContentTypeId, e.SchemaVersion, e.Slug,
        e.CanonicalPath, e.Title, e.Excerpt, e.Fields, e.Version, (ContentStatus)(int)e.Status, e.WorkingRevisionId, e.PublishedRevisionId, e.PublishedAt);
    private static RevisionResponse Revision(ContentRevisionReadModel r) => new(r.Id, r.Version, r.SchemaVersion, r.Slug, r.CanonicalPath,
        r.Title, r.Excerpt, r.Fields, r.CreatedBy?.ToString(), r.CreatedAt);
    private static PublishedEntryResponse Published(PublishedContentEntryReadModel e) => new(e.Id, e.TypeKey, e.Slug, e.CanonicalPath, e.Title, e.Excerpt, e.Fields, e.PublishedAt);
    private static IResult Outcome(EntityResult<ContentAuthoringOutcome> result) => Map(result,
        value => new EntryOutcome(value.Id, value.Version, value.RevisionId, value.Status is null ? null : (ContentStatus)(int)value.Status.Value));
    private static IResult Map<T, R>(EntityResult<T> result, Func<T, R> map) =>
        result.IsSuccess && result.Value is not null ? Results.Ok(map(result.Value)) : Error(result.Errors);
    private static IResult Error(IReadOnlyList<EntityError> errors)
    {
        EntityError? error = errors.FirstOrDefault();
        int status = error?.Code switch
        {
            EntityErrorCode.NotFound => StatusCodes.Status404NotFound,
            EntityErrorCode.Unauthorized => StatusCodes.Status403Forbidden,
            EntityErrorCode.Concurrency or EntityErrorCode.Conflict => StatusCodes.Status409Conflict,
            EntityErrorCode.Unknown => StatusCodes.Status500InternalServerError,
            null => StatusCodes.Status500InternalServerError,
            _ => StatusCodes.Status400BadRequest
        };
        return Results.Problem(statusCode: status, title: "The content operation could not be completed.",
            detail: error?.Code == EntityErrorCode.Unknown ? null : error?.Message);
    }
}
