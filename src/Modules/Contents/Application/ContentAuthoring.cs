using System.Collections.Immutable;
using System.Text.Json;
using Vitarish.Contents.Application.Abstractions;
using Vitarish.Contents.Application.Models;
using Vitarish.Contents.Domain.Entities;
using Vitarish.Contents.Domain.Enums;
using Vitarish.Contents.Domain.Models;
using Vitarish.Entities.Server.Core;
using Vitarish.Entities.Contracts.Models;
using Vitarish.Entities.Server.Abstractions;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Contents.Application;

public sealed class ContentAuthoring(
    IContentAuthoringStore store,
    IRequestContext requestContext,
    IEntityTimeService timeService) : IContentAuthoring
{
    public async Task<EntityResult<ContentAuthoringOutcome>> CreateSiteAsync(
        string key,
        string name,
        string defaultLocale,
        string[] locales,
        CancellationToken cancellationToken)
    {
        EntityError? accessError = ContentRequestAccess.RequireTenantOwner(requestContext, out string tenantId);
        if (accessError is not null)
        {
            return Failure(accessError);
        }

        Site site = new()
        {
            Id = Guid.NewGuid(),
            Key = key,
            Name = name,
            DefaultLocale = defaultLocale,
            Locales = locales
        };
        IReadOnlyList<EntityError> errors = site.Validate();
        if (errors.Count > 0)
        {
            return EntityResult<ContentAuthoringOutcome>.Failure([.. errors]);
        }

        EntityResult writeResult = await store.AddSiteAsync(tenantId, site, cancellationToken);
        return writeResult.IsFailure
            ? EntityResult<ContentAuthoringOutcome>.Failure([.. writeResult.Errors])
            : Success(new ContentAuthoringOutcome(site.Id));
    }

    public async Task<EntityResult<ContentAuthoringOutcome>> UpdateSiteAsync(
        Guid siteId,
        string key,
        string name,
        string defaultLocale,
        string[] locales,
        CancellationToken cancellationToken)
    {
        EntityError? accessError = ContentRequestAccess.RequireTenantOwner(requestContext, out string tenantId);
        if (accessError is not null)
        {
            return Failure(accessError);
        }

        Site? site = await store.GetSiteByIdAsync(tenantId, siteId, cancellationToken);
        if (site is null)
        {
            return Failure(EntityError.NotFound("Site was not found."));
        }

        site.Key = key;
        site.Name = name;
        site.DefaultLocale = defaultLocale;
        site.Locales = locales;
        IReadOnlyList<EntityError> errors = site.Validate();
        if (errors.Count > 0)
        {
            return EntityResult<ContentAuthoringOutcome>.Failure([.. errors]);
        }

        EntityResult writeResult = await store.UpdateSiteAsync(tenantId, site, cancellationToken);
        return writeResult.IsFailure
            ? EntityResult<ContentAuthoringOutcome>.Failure([.. writeResult.Errors])
            : Success(new ContentAuthoringOutcome(site.Id));
    }

    public async Task<EntityResult<ContentAuthoringOutcome>> CreateContentTypeAsync(
        string key,
        string name,
        string? description,
        IReadOnlyList<ContentFieldDefinition> fields,
        CancellationToken cancellationToken)
    {
        EntityError? accessError = ContentRequestAccess.RequireTenantOwner(requestContext, out string tenantId);
        if (accessError is not null)
        {
            return Failure(accessError);
        }

        IReadOnlyList<EntityError> schemaErrors = ContentSchemaRules.Validate(fields);
        if (schemaErrors.Count > 0)
        {
            return EntityResult<ContentAuthoringOutcome>.Failure([.. schemaErrors]);
        }

        ContentType contentType = new()
        {
            Id = Guid.NewGuid(),
            Key = key,
            Name = name,
            Description = description,
            CurrentSchemaVersion = 1
        };
        IReadOnlyList<EntityError> typeErrors = contentType.Validate();
        if (typeErrors.Count > 0)
        {
            return EntityResult<ContentAuthoringOutcome>.Failure([.. typeErrors]);
        }

        ContentTypeSchema schema = new()
        {
            Id = Guid.NewGuid(),
            ContentTypeId = contentType.Id,
            Version = contentType.CurrentSchemaVersion,
            Fields = fields.ToImmutableArray()
        };
        EntityResult writeResult = await store.AddContentTypeAsync(tenantId, contentType, schema, cancellationToken);
        return writeResult.IsFailure
            ? EntityResult<ContentAuthoringOutcome>.Failure([.. writeResult.Errors])
            : Success(new ContentAuthoringOutcome(contentType.Id, contentType.CurrentSchemaVersion));
    }

    public async Task<EntityResult<ContentAuthoringOutcome>> UpdateContentTypeSchemaAsync(
        Guid typeId,
        IReadOnlyList<ContentFieldDefinition> fields,
        CancellationToken cancellationToken)
    {
        EntityError? accessError = ContentRequestAccess.RequireTenantOwner(requestContext, out string tenantId);
        if (accessError is not null)
        {
            return Failure(accessError);
        }

        ContentType? contentType = await store.GetContentTypeByIdAsync(tenantId, typeId, cancellationToken);
        if (contentType is null)
        {
            return Failure(EntityError.NotFound("Content type was not found."));
        }

        ContentTypeSchema? previousSchema = await store.GetContentTypeSchemaAsync(
            tenantId,
            contentType.Id,
            contentType.CurrentSchemaVersion,
            cancellationToken);
        if (previousSchema is null)
        {
            return Failure(EntityError.Unknown("The current content type schema could not be loaded."));
        }

        IReadOnlyList<EntityError> schemaErrors = ContentSchemaRules.Validate(fields);
        if (schemaErrors.Count == 0)
        {
            schemaErrors = ContentSchemaRules.EnsureAdditive(previousSchema.Fields, fields);
        }

        if (schemaErrors.Count > 0)
        {
            return EntityResult<ContentAuthoringOutcome>.Failure([.. schemaErrors]);
        }

        contentType.CurrentSchemaVersion++;
        ContentTypeSchema schema = new()
        {
            Id = Guid.NewGuid(),
            ContentTypeId = contentType.Id,
            Version = contentType.CurrentSchemaVersion,
            Fields = fields.ToImmutableArray()
        };
        EntityResult writeResult = await store.AddContentTypeSchemaAsync(tenantId, contentType, schema, cancellationToken);
        return writeResult.IsFailure
            ? EntityResult<ContentAuthoringOutcome>.Failure([.. writeResult.Errors])
            : Success(new ContentAuthoringOutcome(contentType.Id, contentType.CurrentSchemaVersion));
    }

    public async Task<EntityResult<ContentAuthoringOutcome>> CreateEntryAsync(
        Guid siteId,
        Guid typeId,
        string slug,
        string path,
        string title,
        string? excerpt,
        JsonElement fields,
        CancellationToken cancellationToken)
    {
        EntityError? accessError = ContentRequestAccess.RequireTenantOwner(requestContext, out string tenantId);
        if (accessError is not null)
        {
            return Failure(accessError);
        }

        Site? site = await store.GetSiteByIdAsync(tenantId, siteId, cancellationToken);
        if (site is null)
        {
            return Failure(EntityError.NotFound("Site was not found."));
        }

        ContentType? contentType = await store.GetContentTypeByIdAsync(tenantId, typeId, cancellationToken);
        if (contentType is null)
        {
            return Failure(EntityError.NotFound("Content type was not found."));
        }

        ContentTypeSchema? schema = await store.GetContentTypeSchemaAsync(
            tenantId,
            typeId,
            contentType.CurrentSchemaVersion,
            cancellationToken);
        if (schema is null)
        {
            return Failure(EntityError.Unknown("The current content type schema could not be loaded."));
        }

        IReadOnlyList<EntityError> fieldErrors = await ContentFieldsValidation.ValidateAsync(
            store,
            tenantId,
            schema.Fields,
            fields,
            site.Locales,
            ContentFieldsValidationMode.Draft,
            cancellationToken);
        if (fieldErrors.Count > 0)
        {
            return EntityResult<ContentAuthoringOutcome>.Failure([.. fieldErrors]);
        }

        Guid contentId = Guid.NewGuid();
        ContentItem item = new()
        {
            Id = contentId,
            SiteId = site.Id,
            ContentTypeId = contentType.Id,
            SchemaVersion = contentType.CurrentSchemaVersion,
            Version = 1,
            Status = ContentStatus.Draft,
            Slug = slug,
            CanonicalPath = path,
            Title = title,
            Excerpt = excerpt ?? string.Empty,
            Locale = site.DefaultLocale,
            Fields = fields.Clone(),
            AuthorId = Guid.TryParse(requestContext.UserId, out Guid userId) ? userId : Guid.Empty
        };
        ContentRevision revision = CreateRevision(
            item,
            fields,
            contentType.CurrentSchemaVersion,
            slug,
            path,
            title,
            excerpt ?? string.Empty);
        item.SaveDraft(revision);
        item.CanonicalPath = string.Empty;
        item.Version = 1;

        EntityResult<ContentRevision> writeResult = await store.SaveRevisionAsync(
            tenantId,
            item,
            revision,
            expectedVersion: null,
            outboxOperation: null,
            cancellationToken);
        if (writeResult.IsFailure || writeResult.Value is null)
        {
            return EntityResult<ContentAuthoringOutcome>.Failure([.. writeResult.Errors]);
        }

        return Success(new ContentAuthoringOutcome(item.Id, item.Version, writeResult.Value.Id, item.Status));
    }

    public async Task<EntityResult<ContentAuthoringOutcome>> SaveDraftAsync(
        Guid contentId,
        int expectedVersion,
        string slug,
        string path,
        string title,
        string? excerpt,
        JsonElement fields,
        CancellationToken cancellationToken)
    {
        EntityError? accessError = ContentRequestAccess.RequireTenantOwner(requestContext, out string tenantId);
        if (accessError is not null)
        {
            return Failure(accessError);
        }

        EntityResult<(ContentItem Item, Site Site, ContentType ContentType, ContentTypeSchema Schema)> loaded =
            await LoadEntryContextAsync(tenantId, contentId, cancellationToken);
        if (loaded.IsFailure)
        {
            return EntityResult<ContentAuthoringOutcome>.Failure([.. loaded.Errors]);
        }

        (ContentItem item, Site site, _, ContentTypeSchema schema) = loaded.Value;
        if (item.Version != expectedVersion)
        {
            return Failure(EntityError.Concurrency("Content was changed by another request."));
        }

        IReadOnlyList<EntityError> fieldErrors = await ContentFieldsValidation.ValidateAsync(
            store,
            tenantId,
            schema.Fields,
            fields,
            site.Locales,
            ContentFieldsValidationMode.Draft,
            cancellationToken);
        if (fieldErrors.Count > 0)
        {
            return EntityResult<ContentAuthoringOutcome>.Failure([.. fieldErrors]);
        }

        string? publishedPath = item.PublishedRevisionId is not null ? item.CanonicalPath : null;
        ContentRevision revision = CreateRevision(item, fields, schema.Version, slug, path, title, excerpt ?? string.Empty);
        item.SaveDraft(revision);
        item.CanonicalPath = publishedPath ?? string.Empty;

        item.Version = expectedVersion + 1;
        EntityResult<ContentRevision> writeResult = await store.SaveRevisionAsync(
            tenantId,
            item,
            revision,
            expectedVersion,
            outboxOperation: null,
            cancellationToken);
        if (writeResult.IsFailure || writeResult.Value is null)
        {
            return EntityResult<ContentAuthoringOutcome>.Failure([.. writeResult.Errors]);
        }

        return Success(new ContentAuthoringOutcome(item.Id, item.Version, writeResult.Value.Id, item.Status));
    }

    public async Task<EntityResult<ContentAuthoringOutcome>> PublishAsync(
        Guid contentId,
        Guid revisionId,
        int expectedVersion,
        CancellationToken cancellationToken)
    {
        EntityError? accessError = ContentRequestAccess.RequireTenantOwner(requestContext, out string tenantId);
        if (accessError is not null)
        {
            return Failure(accessError);
        }

        EntityResult<(ContentItem Item, Site Site, ContentType ContentType, ContentTypeSchema Schema)> loaded =
            await LoadEntryContextAsync(tenantId, contentId, cancellationToken);
        if (loaded.IsFailure)
        {
            return EntityResult<ContentAuthoringOutcome>.Failure([.. loaded.Errors]);
        }

        (ContentItem item, Site site, _, _) = loaded.Value;
        if (item.Version != expectedVersion)
        {
            return Failure(EntityError.Concurrency("Content was changed by another request."));
        }

        ContentRevision? revision = await store.GetRevisionByIdAsync(tenantId, contentId, revisionId, cancellationToken);
        if (revision is null)
        {
            return Failure(EntityError.NotFound("Content revision was not found."));
        }

        if (revision.Fields is null || revision.SchemaVersion is not int schemaVersion)
        {
            return Failure(EntityError.Validation("The selected revision has no structured content snapshot."));
        }

        ContentTypeSchema? revisionSchema = await store.GetContentTypeSchemaAsync(
            tenantId,
            item.ContentTypeId!.Value,
            schemaVersion,
            cancellationToken);
        if (revisionSchema is null)
        {
            return Failure(EntityError.Validation("The selected revision schema is unavailable."));
        }

        IReadOnlyList<EntityError> fieldErrors = await ContentFieldsValidation.ValidateAsync(
            store,
            tenantId,
            revisionSchema.Fields,
            revision.Fields.Value,
            site.Locales,
            ContentFieldsValidationMode.Publish,
            cancellationToken);
        if (fieldErrors.Count > 0)
        {
            return EntityResult<ContentAuthoringOutcome>.Failure([.. fieldErrors]);
        }

        if (revision.CanonicalPath is null)
        {
            return Failure(EntityError.Validation("The selected revision has no canonical path."));
        }

        try
        {
            item.Publish(revision, timeService.Now);
        }
        catch (EntityFailureException exception)
        {
            return Failure(exception.Error);
        }

        item.CanonicalPath = revision.CanonicalPath;
        item.Version = expectedVersion + 1;
        EntityResult writeResult = await store.SaveItemAsync(
            tenantId,
            item,
            expectedVersion,
            ContentPublicationOperation.Index,
            revision.Id,
            cancellationToken);
        return writeResult.IsFailure
            ? EntityResult<ContentAuthoringOutcome>.Failure([.. writeResult.Errors])
            : Success(new ContentAuthoringOutcome(item.Id, item.Version, revision.Id, item.Status));
    }

    public async Task<EntityResult<ContentAuthoringOutcome>> UnpublishAsync(
        Guid contentId,
        int expectedVersion,
        CancellationToken cancellationToken)
    {
        EntityError? accessError = ContentRequestAccess.RequireTenantOwner(requestContext, out string tenantId);
        if (accessError is not null)
        {
            return Failure(accessError);
        }

        ContentItem? item = await store.GetEntryByIdAsync(tenantId, contentId, cancellationToken);
        if (item is null || item.SiteId is null || item.ContentTypeId is null || item.Version is null)
        {
            return Failure(EntityError.NotFound("Content entry was not found."));
        }

        if (item.Version != expectedVersion)
        {
            return Failure(EntityError.Concurrency("Content was changed by another request."));
        }

        if (item.Status == ContentStatus.Archived)
        {
            return Failure(EntityError.Validation("Archived content cannot be unpublished."));
        }

        if (item.PublishedRevisionId is null)
        {
            item.CanonicalPath = string.Empty;
            item.Version = expectedVersion + 1;
            EntityResult unchangedResult = await store.SaveItemAsync(
                tenantId,
                item,
                expectedVersion,
                outboxOperation: null,
                revisionId: null,
                cancellationToken);
            return unchangedResult.IsFailure
                ? EntityResult<ContentAuthoringOutcome>.Failure([.. unchangedResult.Errors])
                : Success(new ContentAuthoringOutcome(item.Id, item.Version, item.WorkingRevisionId, item.Status));
        }

        item.Unpublish();
        item.CanonicalPath = string.Empty;
        item.Version = expectedVersion + 1;
        EntityResult writeResult = await store.SaveItemAsync(
            tenantId,
            item,
            expectedVersion,
            ContentPublicationOperation.Remove,
            revisionId: null,
            cancellationToken);
        return writeResult.IsFailure
            ? EntityResult<ContentAuthoringOutcome>.Failure([.. writeResult.Errors])
            : Success(new ContentAuthoringOutcome(item.Id, item.Version, item.WorkingRevisionId, item.Status));
    }

    public async Task<EntityResult<ContentAuthoringOutcome>> ArchiveAsync(
        Guid contentId,
        int expectedVersion,
        CancellationToken cancellationToken)
    {
        EntityError? accessError = ContentRequestAccess.RequireTenantOwner(requestContext, out string tenantId);
        if (accessError is not null)
        {
            return Failure(accessError);
        }

        ContentItem? item = await store.GetEntryByIdAsync(tenantId, contentId, cancellationToken);
        if (item is null || item.SiteId is null || item.ContentTypeId is null || item.Version is null)
        {
            return Failure(EntityError.NotFound("Content entry was not found."));
        }

        if (item.Version != expectedVersion)
        {
            return Failure(EntityError.Concurrency("Content was changed by another request."));
        }

        item.Unpublish();
        item.Archive();
        item.CanonicalPath = string.Empty;
        item.Version = expectedVersion + 1;
        EntityResult writeResult = await store.SaveItemAsync(
            tenantId,
            item,
            expectedVersion,
            ContentPublicationOperation.Remove,
            revisionId: null,
            cancellationToken);
        return writeResult.IsFailure
            ? EntityResult<ContentAuthoringOutcome>.Failure([.. writeResult.Errors])
            : Success(new ContentAuthoringOutcome(item.Id, item.Version, item.WorkingRevisionId, item.Status));
    }

    public async Task<EntityResult<ContentAuthoringOutcome>> RestoreRevisionAsync(
        Guid contentId,
        Guid revisionId,
        int expectedVersion,
        CancellationToken cancellationToken)
    {
        EntityError? accessError = ContentRequestAccess.RequireTenantOwner(requestContext, out string tenantId);
        if (accessError is not null)
        {
            return Failure(accessError);
        }

        EntityResult<(ContentItem Item, Site Site, ContentType ContentType, ContentTypeSchema Schema)> loaded =
            await LoadEntryContextAsync(tenantId, contentId, cancellationToken);
        if (loaded.IsFailure)
        {
            return EntityResult<ContentAuthoringOutcome>.Failure([.. loaded.Errors]);
        }

        (ContentItem item, Site site, _, ContentTypeSchema schema) = loaded.Value;
        if (item.Version != expectedVersion)
        {
            return Failure(EntityError.Concurrency("Content was changed by another request."));
        }

        ContentRevision? sourceRevision = await store.GetRevisionByIdAsync(tenantId, contentId, revisionId, cancellationToken);
        if (sourceRevision is null)
        {
            return Failure(EntityError.NotFound("Content revision was not found."));
        }

        if (sourceRevision.Fields is null)
        {
            return Failure(EntityError.Validation("The selected revision has no structured content snapshot."));
        }

        if (sourceRevision.Slug is null || sourceRevision.CanonicalPath is null || sourceRevision.Title is null)
        {
            return Failure(EntityError.Validation("The selected revision has incomplete entry metadata."));
        }

        JsonElement fields = sourceRevision.Fields.Value.Clone();
        IReadOnlyList<EntityError> fieldErrors = await ContentFieldsValidation.ValidateAsync(
            store,
            tenantId,
            schema.Fields,
            fields,
            site.Locales,
            ContentFieldsValidationMode.Draft,
            cancellationToken);
        if (fieldErrors.Count > 0)
        {
            return EntityResult<ContentAuthoringOutcome>.Failure([.. fieldErrors]);
        }

        string? publishedPath = item.PublishedRevisionId is not null ? item.CanonicalPath : null;
        ContentRevision restoredRevision = CreateRevision(
            item,
            fields,
            schema.Version,
            sourceRevision.Slug,
            sourceRevision.CanonicalPath,
            sourceRevision.Title,
            sourceRevision.Excerpt ?? string.Empty);
        item.SaveDraft(restoredRevision);
        item.CanonicalPath = publishedPath ?? string.Empty;

        item.Version = expectedVersion + 1;
        EntityResult<ContentRevision> writeResult = await store.SaveRevisionAsync(
            tenantId,
            item,
            restoredRevision,
            expectedVersion,
            outboxOperation: null,
            cancellationToken);
        if (writeResult.IsFailure || writeResult.Value is null)
        {
            return EntityResult<ContentAuthoringOutcome>.Failure([.. writeResult.Errors]);
        }

        return Success(new ContentAuthoringOutcome(item.Id, item.Version, writeResult.Value.Id, item.Status));
    }

    private async Task<EntityResult<(ContentItem Item, Site Site, ContentType ContentType, ContentTypeSchema Schema)>> LoadEntryContextAsync(
        string tenantId,
        Guid contentId,
        CancellationToken cancellationToken)
    {
        ContentItem? item = await store.GetEntryByIdAsync(tenantId, contentId, cancellationToken);
        if (item is null || item.SiteId is not Guid siteId || item.ContentTypeId is not Guid typeId || item.SchemaVersion is null)
        {
            return EntityResult<(ContentItem, Site, ContentType, ContentTypeSchema)>.Failure(EntityError.NotFound("Content entry was not found."));
        }

        Site? site = await store.GetSiteByIdAsync(tenantId, siteId, cancellationToken);
        ContentType? contentType = await store.GetContentTypeByIdAsync(tenantId, typeId, cancellationToken);
        ContentTypeSchema? schema = contentType is null
            ? null
            : await store.GetContentTypeSchemaAsync(tenantId, typeId, contentType.CurrentSchemaVersion, cancellationToken);
        if (site is null || contentType is null || schema is null)
        {
            return EntityResult<(ContentItem, Site, ContentType, ContentTypeSchema)>.Failure(EntityError.NotFound("Content entry schema or site was not found."));
        }

        return EntityResult<(ContentItem, Site, ContentType, ContentTypeSchema)>.Success((item, site, contentType, schema));
    }

    private ContentRevision CreateRevision(
        ContentItem item,
        JsonElement fields,
        int schemaVersion,
        string slug,
        string path,
        string title,
        string excerpt)
    {
        string? createdBy = requestContext.UserId;
        return new ContentRevision
        {
            Id = Guid.NewGuid(),
            ContentId = item.Id,
            Fields = fields.Clone(),
            Title = title,
            Slug = slug,
            CanonicalPath = path,
            Excerpt = excerpt,
            SchemaVersion = schemaVersion,
            CreatedBy = createdBy
        };
    }

    private static EntityResult<ContentAuthoringOutcome> Success(ContentAuthoringOutcome outcome) =>
        EntityResult<ContentAuthoringOutcome>.Success(outcome);

    private static EntityResult<ContentAuthoringOutcome> Failure(EntityError error) =>
        EntityResult<ContentAuthoringOutcome>.Failure(error);
}
