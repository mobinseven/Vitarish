using Vitarish.Contents.Application.Abstractions;
using Vitarish.Contents.Application.Models;
using Vitarish.Contents.Domain.Entities;
using Vitarish.Contents.Domain.Enums;
using Vitarish.Entities.Contracts.Models;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Contents.Application;

public sealed class ContentAuthoringQueries(
    IContentAuthoringStore store,
    IRequestContext requestContext) : IContentAuthoringQueries
{
    public async Task<EntityResult<IReadOnlyList<SiteReadModel>>> ListSitesAsync(CancellationToken cancellationToken)
    {
        EntityError? accessError = ContentRequestAccess.RequireTenantOwner(requestContext, out string tenantId);
        if (accessError is not null)
        {
            return EntityResult<IReadOnlyList<SiteReadModel>>.Failure(accessError);
        }

        IReadOnlyList<Site> sites = await store.ListSitesAsync(tenantId, cancellationToken);
        return EntityResult<IReadOnlyList<SiteReadModel>>.Success(sites.Select(ToReadModel).ToArray());
    }

    public async Task<EntityResult<IReadOnlyList<ContentTypeReadModel>>> ListContentTypesAsync(CancellationToken cancellationToken)
    {
        EntityError? accessError = ContentRequestAccess.RequireTenantOwner(requestContext, out string tenantId);
        if (accessError is not null)
        {
            return EntityResult<IReadOnlyList<ContentTypeReadModel>>.Failure(accessError);
        }

        IReadOnlyList<ContentType> contentTypes = await store.ListContentTypesAsync(tenantId, cancellationToken);
        List<ContentTypeReadModel> results = [];
        foreach (ContentType contentType in contentTypes)
        {
            ContentTypeSchema? schema = await store.GetContentTypeSchemaAsync(
                tenantId,
                contentType.Id,
                contentType.CurrentSchemaVersion,
                cancellationToken);
            if (schema is null)
            {
                return EntityResult<IReadOnlyList<ContentTypeReadModel>>.Failure(
                    EntityError.Unknown("The current content type schema could not be loaded."));
            }

            results.Add(ToReadModel(contentType, schema));
        }

        return EntityResult<IReadOnlyList<ContentTypeReadModel>>.Success(results);
    }

    public async Task<EntityResult<ContentTypeReadModel>> GetContentTypeAsync(Guid typeId, CancellationToken cancellationToken)
    {
        EntityError? accessError = ContentRequestAccess.RequireTenantOwner(requestContext, out string tenantId);
        if (accessError is not null)
        {
            return EntityResult<ContentTypeReadModel>.Failure(accessError);
        }

        ContentType? contentType = await store.GetContentTypeByIdAsync(tenantId, typeId, cancellationToken);
        if (contentType is null)
        {
            return EntityResult<ContentTypeReadModel>.Failure(EntityError.NotFound("Content type was not found."));
        }

        ContentTypeSchema? schema = await store.GetContentTypeSchemaAsync(
            tenantId,
            contentType.Id,
            contentType.CurrentSchemaVersion,
            cancellationToken);
        if (schema is null)
        {
            return EntityResult<ContentTypeReadModel>.Failure(EntityError.Unknown("The current content type schema could not be loaded."));
        }

        return EntityResult<ContentTypeReadModel>.Success(ToReadModel(contentType, schema));
    }

    public async Task<EntityResult<IReadOnlyList<ContentEntryReadModel>>> ListEntriesAsync(
        Guid siteId,
        Guid? typeId,
        ContentStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        EntityError? accessError = ContentRequestAccess.RequireTenantOwner(requestContext, out string tenantId);
        if (accessError is not null)
        {
            return EntityResult<IReadOnlyList<ContentEntryReadModel>>.Failure(accessError);
        }

        if (skip < 0 || take is < 1 or > 100)
        {
            return EntityResult<IReadOnlyList<ContentEntryReadModel>>.Failure(
                EntityError.Validation("Entry pagination requires a non-negative skip and a take between 1 and 100."));
        }

        if (await store.GetSiteByIdAsync(tenantId, siteId, cancellationToken) is null)
        {
            return EntityResult<IReadOnlyList<ContentEntryReadModel>>.Failure(EntityError.NotFound("Site was not found."));
        }

        IReadOnlyList<ContentItem> items = await store.ListEntriesAsync(
            tenantId,
            siteId,
            typeId,
            status,
            skip,
            take,
            cancellationToken);

        List<ContentEntryReadModel> results = [];
        foreach (ContentItem item in items)
        {
            if (!HasStructuredContent(item))
            {
                return EntityResult<IReadOnlyList<ContentEntryReadModel>>.Failure(
                    EntityError.Unknown("The structured content entry is incomplete."));
            }

            if (item.WorkingRevisionId is not Guid revisionId)
            {
                return EntityResult<IReadOnlyList<ContentEntryReadModel>>.Failure(
                    EntityError.Unknown("The working content revision could not be loaded."));
            }

            ContentRevision? workingRevision = await store.GetRevisionByIdAsync(tenantId, item.Id, revisionId, cancellationToken);
            if (workingRevision is null || !HasRevisionSnapshot(workingRevision))
            {
                return EntityResult<IReadOnlyList<ContentEntryReadModel>>.Failure(
                    EntityError.Unknown("The working content revision could not be loaded."));
            }

            results.Add(ToReadModel(item, workingRevision));
        }

        return EntityResult<IReadOnlyList<ContentEntryReadModel>>.Success(results);
    }

    public async Task<EntityResult<ContentEntryReadModel>> GetEntryAsync(Guid contentId, CancellationToken cancellationToken)
    {
        EntityError? accessError = ContentRequestAccess.RequireTenantOwner(requestContext, out string tenantId);
        if (accessError is not null)
        {
            return EntityResult<ContentEntryReadModel>.Failure(accessError);
        }

        ContentItem? contentItem = await store.GetEntryByIdAsync(tenantId, contentId, cancellationToken);
        if (contentItem is null || !HasStructuredContent(contentItem))
        {
            return EntityResult<ContentEntryReadModel>.Failure(EntityError.NotFound("Content entry was not found."));
        }

        if (contentItem.WorkingRevisionId is not Guid revisionId)
        {
            return EntityResult<ContentEntryReadModel>.Failure(EntityError.Unknown("The working content revision could not be loaded."));
        }

        ContentRevision? workingRevision = await store.GetRevisionByIdAsync(tenantId, contentId, revisionId, cancellationToken);
        if (workingRevision is null || !HasRevisionSnapshot(workingRevision))
        {
            return EntityResult<ContentEntryReadModel>.Failure(EntityError.Unknown("The working content revision could not be loaded."));
        }

        return EntityResult<ContentEntryReadModel>.Success(ToReadModel(contentItem, workingRevision));
    }

    public async Task<EntityResult<IReadOnlyList<ContentRevisionReadModel>>> ListRevisionsAsync(
        Guid contentId,
        CancellationToken cancellationToken)
    {
        EntityError? accessError = ContentRequestAccess.RequireTenantOwner(requestContext, out string tenantId);
        if (accessError is not null)
        {
            return EntityResult<IReadOnlyList<ContentRevisionReadModel>>.Failure(accessError);
        }

        ContentItem? contentItem = await store.GetEntryByIdAsync(tenantId, contentId, cancellationToken);
        if (contentItem is null || !HasStructuredContent(contentItem))
        {
            return EntityResult<IReadOnlyList<ContentRevisionReadModel>>.Failure(EntityError.NotFound("Content entry was not found."));
        }

        IReadOnlyList<ContentRevision> revisions = await store.ListRevisionsAsync(tenantId, contentId, cancellationToken);
        if (revisions.Any(revision => !HasRevisionSnapshot(revision)))
        {
            return EntityResult<IReadOnlyList<ContentRevisionReadModel>>.Failure(
                EntityError.Unknown("A content revision snapshot could not be loaded."));
        }

        return EntityResult<IReadOnlyList<ContentRevisionReadModel>>.Success(revisions.Select(ToReadModel).ToArray());
    }

    public async Task<EntityResult<ContentRevisionReadModel>> GetRevisionAsync(
        Guid contentId,
        Guid revisionId,
        CancellationToken cancellationToken)
    {
        EntityError? accessError = ContentRequestAccess.RequireTenantOwner(requestContext, out string tenantId);
        if (accessError is not null)
        {
            return EntityResult<ContentRevisionReadModel>.Failure(accessError);
        }

        ContentItem? contentItem = await store.GetEntryByIdAsync(tenantId, contentId, cancellationToken);
        if (contentItem is null || !HasStructuredContent(contentItem))
        {
            return EntityResult<ContentRevisionReadModel>.Failure(EntityError.NotFound("Content entry was not found."));
        }

        ContentRevision? revision = await store.GetRevisionByIdAsync(tenantId, contentId, revisionId, cancellationToken);
        return revision is null
            ? EntityResult<ContentRevisionReadModel>.Failure(EntityError.NotFound("Content revision was not found."))
            : !HasRevisionSnapshot(revision)
                ? EntityResult<ContentRevisionReadModel>.Failure(EntityError.Unknown("The content revision snapshot could not be loaded."))
            : EntityResult<ContentRevisionReadModel>.Success(ToReadModel(revision));
    }

    private static bool HasStructuredContent(ContentItem item) =>
        item.SiteId is not null && item.ContentTypeId is not null && item.SchemaVersion is not null && item.Version is not null;

    private static bool HasRevisionSnapshot(ContentRevision revision) =>
        revision.Fields is not null &&
        revision.SchemaVersion is not null &&
        revision.Title is not null &&
        revision.Slug is not null &&
        revision.CanonicalPath is not null;

    private static SiteReadModel ToReadModel(Site site) =>
        new(site.Id, site.Key, site.Name, site.DefaultLocale, site.Locales);

    private static ContentTypeReadModel ToReadModel(ContentType contentType, ContentTypeSchema schema) =>
        new(contentType.Id, contentType.Key, contentType.Name, contentType.Description, contentType.CurrentSchemaVersion, schema.Fields);

    private static ContentEntryReadModel ToReadModel(ContentItem item, ContentRevision workingRevision) =>
        new(
            item.Id,
            item.SiteId!.Value,
            item.ContentTypeId!.Value,
            item.SchemaVersion!.Value,
            item.Version!.Value,
            workingRevision.Slug!,
            workingRevision.CanonicalPath!,
            workingRevision.Title!,
            workingRevision.Excerpt ?? string.Empty,
            workingRevision.Fields!.Value.Clone(),
            item.WorkingRevisionId,
            item.PublishedRevisionId,
            item.Status,
            item.PublishedAt);

    private static ContentRevisionReadModel ToReadModel(ContentRevision revision) =>
        new(
            revision.Id,
            revision.ContentId,
            revision.Version,
            revision.Title!,
            revision.Slug!,
            revision.CanonicalPath!,
            revision.Excerpt ?? string.Empty,
            revision.Fields!.Value.Clone(),
            revision.SchemaVersion!.Value,
            revision.CreatedBy,
            revision.CreatedAt);
}
