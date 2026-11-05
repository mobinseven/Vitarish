using Vitarish.Contents.Application.Abstractions;
using Vitarish.Contents.Application.Models;
using Vitarish.Contents.Domain.Entities;
using Vitarish.Contents.Domain.Enums;
using Vitarish.Entities.Contracts.Models;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Contents.Application;

public sealed class ContentDelivery(
    IContentAuthoringStore store,
    IRequestContext requestContext)
{
    public async Task<EntityResult<SiteReadModel>> GetSiteAsync(string siteKey, CancellationToken cancellationToken)
    {
        if (!ContentRequestAccess.TryGetTenant(requestContext, out string tenantId))
        {
            return EntityResult<SiteReadModel>.Failure(EntityError.NotFound("Site was not found."));
        }

        Site? site = await store.GetSiteByKeyAsync(tenantId, siteKey, cancellationToken);
        return site is null
            ? EntityResult<SiteReadModel>.Failure(EntityError.NotFound("Site was not found."))
            : EntityResult<SiteReadModel>.Success(new SiteReadModel(site.Id, site.Key, site.Name, site.DefaultLocale, site.Locales));
    }

    public async Task<EntityResult<PublishedContentEntryReadModel>> GetPublishedEntryByPathAsync(
        string siteKey,
        string path,
        CancellationToken cancellationToken)
    {
        if (!ContentRequestAccess.TryGetTenant(requestContext, out string tenantId))
        {
            return EntityResult<PublishedContentEntryReadModel>.Failure(EntityError.NotFound("Published content was not found."));
        }

        Site? site = await store.GetSiteByKeyAsync(tenantId, siteKey, cancellationToken);
        if (site is null)
        {
            return EntityResult<PublishedContentEntryReadModel>.Failure(EntityError.NotFound("Published content was not found."));
        }

        PublishedContentSnapshot? snapshot = await store.GetPublishedEntryByPathAsync(tenantId, site.Id, path, cancellationToken);
        if (snapshot is null || !IsPublishedSnapshot(snapshot) || snapshot.ContentItem.ContentTypeId is not Guid typeId)
        {
            return EntityResult<PublishedContentEntryReadModel>.Failure(EntityError.NotFound("Published content was not found."));
        }

        ContentType? contentType = await store.GetContentTypeByIdAsync(tenantId, typeId, cancellationToken);
        return contentType is null
            ? EntityResult<PublishedContentEntryReadModel>.Failure(EntityError.NotFound("Published content was not found."))
            : EntityResult<PublishedContentEntryReadModel>.Success(ToReadModel(snapshot, contentType.Key));
    }

    public async Task<EntityResult<IReadOnlyList<PublishedContentEntryReadModel>>> ListPublishedEntriesAsync(
        string siteKey,
        string typeKey,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        if (!ContentRequestAccess.TryGetTenant(requestContext, out string tenantId))
        {
            return EntityResult<IReadOnlyList<PublishedContentEntryReadModel>>.Failure(EntityError.NotFound("Published content was not found."));
        }

        if (skip < 0 || take is < 1 or > 50)
        {
            return EntityResult<IReadOnlyList<PublishedContentEntryReadModel>>.Failure(
                EntityError.Validation("Published entry pagination requires a non-negative skip and a take between 1 and 50."));
        }

        Site? site = await store.GetSiteByKeyAsync(tenantId, siteKey, cancellationToken);
        ContentType? contentType = await store.GetContentTypeByKeyAsync(tenantId, typeKey, cancellationToken);
        if (site is null || contentType is null)
        {
            return EntityResult<IReadOnlyList<PublishedContentEntryReadModel>>.Failure(EntityError.NotFound("Published content was not found."));
        }

        IReadOnlyList<PublishedContentSnapshot> snapshots = await store.ListPublishedEntriesAsync(
            tenantId,
            site.Id,
            contentType.Id,
            skip,
            take,
            cancellationToken);
        IReadOnlyList<PublishedContentEntryReadModel> entries = snapshots
            .Where(IsPublishedSnapshot)
            .Select(snapshot => ToReadModel(snapshot, contentType.Key))
            .ToArray();
        return EntityResult<IReadOnlyList<PublishedContentEntryReadModel>>.Success(entries);
    }

    private static bool IsPublishedSnapshot(PublishedContentSnapshot snapshot) =>
        snapshot.ContentItem.Status == ContentStatus.Published &&
        snapshot.ContentItem.PublishedRevisionId == snapshot.Revision.Id &&
        snapshot.ContentItem.PublishedAt is not null &&
        snapshot.ContentItem.SiteId is not null &&
        snapshot.ContentItem.ContentTypeId is not null &&
        snapshot.ContentItem.SchemaVersion is not null &&
        snapshot.ContentItem.Version is not null &&
        snapshot.Revision.Fields is not null &&
        snapshot.Revision.SchemaVersion is not null &&
        snapshot.Revision.Title is not null &&
        snapshot.Revision.Slug is not null &&
        snapshot.Revision.CanonicalPath is not null;

    private static PublishedContentEntryReadModel ToReadModel(PublishedContentSnapshot snapshot, string typeKey)
    {
        ContentItem item = snapshot.ContentItem;
        ContentRevision revision = snapshot.Revision;
        return new PublishedContentEntryReadModel(
            item.Id,
            item.SiteId!.Value,
            item.ContentTypeId!.Value,
            typeKey,
            revision.SchemaVersion!.Value,
            revision.Version,
            revision.Slug!,
            revision.CanonicalPath!,
            revision.Title!,
            revision.Excerpt ?? string.Empty,
            revision.Fields!.Value.Clone(),
            item.PublishedAt!.Value);
    }
}
