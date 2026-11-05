using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using Vitarish.Contents.Application.Abstractions;
using Vitarish.Contents.Application.Models;
using Vitarish.Entities.Contracts.Models;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Contents.Infrastructure.Stores;

internal sealed class ContentAuthoringStore(
    ContentsDbContext dbContext,
    IRequestContext requestContext) : IContentAuthoringStore
{
    public async Task<IReadOnlyList<Site>> ListSitesAsync(string tenantId, CancellationToken cancellationToken)
    {
        EnsureTenant(tenantId);
        return await dbContext.Sites.AsNoTracking()
            .Where(site => EF.Property<string>(site, nameof(ContentPublicationOutboxRecord.TenantId)) == tenantId)
            .OrderBy(site => site.Key)
            .ToListAsync(cancellationToken);
    }

    public async Task<Site?> GetSiteByIdAsync(string tenantId, Guid siteId, CancellationToken cancellationToken)
    {
        EnsureTenant(tenantId);
        return await dbContext.Sites.AsNoTracking()
            .SingleOrDefaultAsync(site =>
                EF.Property<string>(site, nameof(ContentPublicationOutboxRecord.TenantId)) == tenantId && site.Id == siteId,
                cancellationToken);
    }

    public async Task<Site?> GetSiteByKeyAsync(string tenantId, string siteKey, CancellationToken cancellationToken)
    {
        EnsureTenant(tenantId);
        return await dbContext.Sites.AsNoTracking()
            .SingleOrDefaultAsync(site =>
                EF.Property<string>(site, nameof(ContentPublicationOutboxRecord.TenantId)) == tenantId && site.Key == siteKey,
                cancellationToken);
    }

    public async Task<EntityResult> AddSiteAsync(string tenantId, Site site, CancellationToken cancellationToken)
    {
        EnsureTenant(tenantId);
        try
        {
            _ = await dbContext.Sites.AddAsync(site, cancellationToken);
            _ = await dbContext.SaveChangesAsync(cancellationToken);
            return EntityResult.Success();
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            dbContext.ChangeTracker.Clear();
            return EntityResult.Failure(EntityError.Conflict("A site with this key already exists."));
        }
    }

    public async Task<EntityResult> UpdateSiteAsync(string tenantId, Site site, CancellationToken cancellationToken)
    {
        EnsureTenant(tenantId);
        Site? storedSite = await dbContext.Sites.SingleOrDefaultAsync(candidate =>
            EF.Property<string>(candidate, nameof(ContentPublicationOutboxRecord.TenantId)) == tenantId && candidate.Id == site.Id,
            cancellationToken);
        if (storedSite is null)
        {
            return EntityResult.Failure(EntityError.NotFound("Site was not found."));
        }

        storedSite.Key = site.Key;
        storedSite.Name = site.Name;
        storedSite.DefaultLocale = site.DefaultLocale;
        storedSite.Locales = [.. site.Locales];
        try
        {
            _ = await dbContext.SaveChangesAsync(cancellationToken);
            return EntityResult.Success();
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            dbContext.ChangeTracker.Clear();
            return EntityResult.Failure(EntityError.Conflict("A site with this key already exists."));
        }
    }

    public async Task<IReadOnlyList<ContentType>> ListContentTypesAsync(string tenantId, CancellationToken cancellationToken)
    {
        EnsureTenant(tenantId);
        return await dbContext.ContentTypes.AsNoTracking()
            .Where(type => EF.Property<string>(type, nameof(ContentPublicationOutboxRecord.TenantId)) == tenantId)
            .OrderBy(type => type.Key)
            .ToListAsync(cancellationToken);
    }

    public async Task<ContentType?> GetContentTypeByIdAsync(string tenantId, Guid typeId, CancellationToken cancellationToken)
    {
        EnsureTenant(tenantId);
        return await dbContext.ContentTypes.AsNoTracking()
            .SingleOrDefaultAsync(type =>
                EF.Property<string>(type, nameof(ContentPublicationOutboxRecord.TenantId)) == tenantId && type.Id == typeId,
                cancellationToken);
    }

    public async Task<ContentType?> GetContentTypeByKeyAsync(string tenantId, string typeKey, CancellationToken cancellationToken)
    {
        EnsureTenant(tenantId);
        return await dbContext.ContentTypes.AsNoTracking()
            .SingleOrDefaultAsync(type =>
                EF.Property<string>(type, nameof(ContentPublicationOutboxRecord.TenantId)) == tenantId && type.Key == typeKey,
                cancellationToken);
    }

    public async Task<ContentTypeSchema?> GetContentTypeSchemaAsync(
        string tenantId,
        Guid typeId,
        int version,
        CancellationToken cancellationToken)
    {
        EnsureTenant(tenantId);
        return await dbContext.ContentTypeSchemas.AsNoTracking()
            .SingleOrDefaultAsync(schema =>
                EF.Property<string>(schema, nameof(ContentPublicationOutboxRecord.TenantId)) == tenantId &&
                schema.ContentTypeId == typeId && schema.Version == version,
                cancellationToken);
    }

    public async Task<EntityResult> AddContentTypeAsync(
        string tenantId,
        ContentType contentType,
        ContentTypeSchema schema,
        CancellationToken cancellationToken)
    {
        EnsureTenant(tenantId);
        await using IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            _ = await dbContext.ContentTypes.AddAsync(contentType, cancellationToken);
            _ = await dbContext.ContentTypeSchemas.AddAsync(schema, cancellationToken);
            _ = await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return EntityResult.Success();
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            dbContext.ChangeTracker.Clear();
            await transaction.RollbackAsync(cancellationToken);
            return EntityResult.Failure(EntityError.Conflict("A content type with this key already exists."));
        }
    }

    public async Task<EntityResult> AddContentTypeSchemaAsync(
        string tenantId,
        ContentType contentType,
        ContentTypeSchema schema,
        CancellationToken cancellationToken)
    {
        EnsureTenant(tenantId);
        await using IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        ContentType? storedType = await dbContext.ContentTypes.SingleOrDefaultAsync(candidate =>
            EF.Property<string>(candidate, nameof(ContentPublicationOutboxRecord.TenantId)) == tenantId && candidate.Id == contentType.Id,
            cancellationToken);
        if (storedType is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return EntityResult.Failure(EntityError.NotFound("Content type was not found."));
        }

        storedType.CurrentSchemaVersion = contentType.CurrentSchemaVersion;
        _ = await dbContext.ContentTypeSchemas.AddAsync(schema, cancellationToken);
        try
        {
            _ = await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return EntityResult.Success();
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            dbContext.ChangeTracker.Clear();
            await transaction.RollbackAsync(cancellationToken);
            return EntityResult.Failure(EntityError.Conflict("The content type schema version already exists."));
        }
    }

    public async Task<IReadOnlyList<ContentItem>> ListEntriesAsync(
        string tenantId,
        Guid siteId,
        Guid? typeId,
        ContentStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        EnsureTenant(tenantId);
        IQueryable<ContentItem> query = dbContext.ContentItems.AsNoTracking()
            .Where(item => EF.Property<string>(item, nameof(ContentPublicationOutboxRecord.TenantId)) == tenantId && item.SiteId == siteId);
        if (typeId is not null)
        {
            query = query.Where(item => item.ContentTypeId == typeId);
        }

        if (status is not null)
        {
            query = query.Where(item => item.Status == status);
        }

        return await query.OrderByDescending(item => item.UpdatedAt ?? item.CreatedAt)
            .ThenBy(item => item.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<ContentItem?> GetEntryByIdAsync(string tenantId, Guid contentId, CancellationToken cancellationToken)
    {
        EnsureTenant(tenantId);
        return await dbContext.ContentItems.AsNoTracking()
            .SingleOrDefaultAsync(item =>
                EF.Property<string>(item, nameof(ContentPublicationOutboxRecord.TenantId)) == tenantId && item.Id == contentId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<ContentRevision>> ListRevisionsAsync(
        string tenantId,
        Guid contentId,
        CancellationToken cancellationToken)
    {
        EnsureTenant(tenantId);
        return await dbContext.ContentRevisions.AsNoTracking()
            .Where(revision =>
                EF.Property<string>(revision, nameof(ContentPublicationOutboxRecord.TenantId)) == tenantId && revision.ContentId == contentId)
            .OrderByDescending(revision => revision.Version)
            .ToListAsync(cancellationToken);
    }

    public async Task<ContentRevision?> GetRevisionByIdAsync(
        string tenantId,
        Guid contentId,
        Guid revisionId,
        CancellationToken cancellationToken)
    {
        EnsureTenant(tenantId);
        return await dbContext.ContentRevisions.AsNoTracking()
            .SingleOrDefaultAsync(revision =>
                EF.Property<string>(revision, nameof(ContentPublicationOutboxRecord.TenantId)) == tenantId &&
                revision.ContentId == contentId && revision.Id == revisionId,
                cancellationToken);
    }

    public async Task<PublishedContentSnapshot?> GetPublishedEntryByPathAsync(
        string tenantId,
        Guid siteId,
        string path,
        CancellationToken cancellationToken)
    {
        EnsureTenant(tenantId);
        var entry = await (
            from item in dbContext.ContentItems.AsNoTracking()
            join revision in dbContext.ContentRevisions.AsNoTracking()
                on item.PublishedRevisionId equals (Guid?)revision.Id
            join site in dbContext.Sites.AsNoTracking()
                on item.SiteId equals (Guid?)site.Id
            where EF.Property<string>(item, nameof(ContentPublicationOutboxRecord.TenantId)) == tenantId &&
                  EF.Property<string>(revision, nameof(ContentPublicationOutboxRecord.TenantId)) == tenantId &&
                  EF.Property<string>(site, nameof(ContentPublicationOutboxRecord.TenantId)) == tenantId &&
                  item.SiteId == siteId && item.Status == ContentStatus.Published &&
                  item.PublishedRevisionId != null && revision.ContentId == item.Id &&
                  revision.CanonicalPath == path
            select new { Site = site, Item = item, Revision = revision })
            .FirstOrDefaultAsync(cancellationToken);

        return entry is null ? null : new PublishedContentSnapshot(entry.Site, entry.Item, entry.Revision);
    }

    public async Task<IReadOnlyList<PublishedContentSnapshot>> ListPublishedEntriesAsync(
        string tenantId,
        Guid siteId,
        Guid typeId,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        EnsureTenant(tenantId);
        var entries = await (
            from item in dbContext.ContentItems.AsNoTracking()
            join revision in dbContext.ContentRevisions.AsNoTracking()
                on item.PublishedRevisionId equals (Guid?)revision.Id
            join site in dbContext.Sites.AsNoTracking()
                on item.SiteId equals (Guid?)site.Id
            where EF.Property<string>(item, nameof(ContentPublicationOutboxRecord.TenantId)) == tenantId &&
                  EF.Property<string>(revision, nameof(ContentPublicationOutboxRecord.TenantId)) == tenantId &&
                  EF.Property<string>(site, nameof(ContentPublicationOutboxRecord.TenantId)) == tenantId &&
                  item.SiteId == siteId && item.Status == ContentStatus.Published &&
                  item.PublishedRevisionId != null && revision.ContentId == item.Id && item.ContentTypeId == typeId
            orderby item.PublishedAt descending, item.Id
            select new { Site = site, Item = item, Revision = revision })
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return entries
            .Select(entry => new PublishedContentSnapshot(entry.Site, entry.Item, entry.Revision))
            .ToList();
    }

    public Task<EntityResult<ContentRevision>> SaveRevisionAsync(
        string tenantId,
        ContentItem contentItem,
        ContentRevision revision,
        int? expectedVersion,
        ContentPublicationOperation? outboxOperation,
        CancellationToken cancellationToken) =>
        SaveRevisionCoreAsync(
            tenantId,
            contentItem,
            revision,
            expectedVersion,
            outboxOperation,
            publishSeed: false,
            cancellationToken: cancellationToken);

    public async Task<EntityResult> SavePublishedSeedAsync(
        string tenantId,
        ContentItem contentItem,
        ContentRevision revision,
        CancellationToken cancellationToken)
    {
        EntityResult<ContentRevision> result = await SaveRevisionCoreAsync(
            tenantId,
            contentItem,
            revision,
            expectedVersion: null,
            outboxOperation: ContentPublicationOperation.Index,
            publishSeed: true,
            cancellationToken: cancellationToken);
        return result.IsSuccess
            ? EntityResult.Success()
            : EntityResult.Failure([.. result.Errors]);
    }

    private async Task<EntityResult<ContentRevision>> SaveRevisionCoreAsync(
        string tenantId,
        ContentItem contentItem,
        ContentRevision revision,
        int? expectedVersion,
        ContentPublicationOperation? outboxOperation,
        bool publishSeed,
        CancellationToken cancellationToken)
    {
        EnsureTenant(tenantId);
        await using IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            if (expectedVersion is null)
            {
                if (contentItem.SiteId is null || contentItem.ContentTypeId is null)
                {
                    return await RollbackFailureAsync<ContentRevision>(transaction,
                        EntityError.Validation("Structured entries require a site and content type."), cancellationToken);
                }

                contentItem.WorkingRevisionId = null;
                contentItem.PublishedRevisionId = null;
                contentItem.Version = 1;
                _ = await dbContext.ContentItems.AddAsync(contentItem, cancellationToken);
                _ = await dbContext.SaveChangesAsync(cancellationToken);
            }
            else
            {
                ContentItem? storedItem = await ReadItemForUpdateAsync(tenantId, contentItem.Id, cancellationToken);
                if (storedItem is null)
                {
                    return await RollbackFailureAsync<ContentRevision>(transaction,
                        EntityError.NotFound("Content entry was not found."), cancellationToken);
                }

                if (storedItem.Version != expectedVersion)
                {
                    dbContext.ChangeTracker.Clear();
                    return await RollbackFailureAsync<ContentRevision>(transaction,
                        EntityError.Concurrency("The content entry was changed by another request."), cancellationToken);
                }

                ContentItem trackedItem = await LoadTrackedItemAsync(tenantId, contentItem.Id, cancellationToken);
                CopyContentItem(contentItem, trackedItem);
                trackedItem.WorkingRevisionId = null;
                trackedItem.Version = expectedVersion.Value + 1;
                await dbContext.SaveChangesAsync(cancellationToken);
                contentItem = trackedItem;
            }

            int nextRevisionVersion = (await dbContext.ContentRevisions
                .Where(candidate =>
                    EF.Property<string>(candidate, nameof(ContentPublicationOutboxRecord.TenantId)) == tenantId && candidate.ContentId == contentItem.Id)
                .Select(candidate => (int?)candidate.Version)
                .MaxAsync(cancellationToken) ?? 0) + 1;

            ContentRevision persistedRevision = new()
            {
                Id = revision.Id,
                ContentId = contentItem.Id,
                Version = nextRevisionVersion,
                Snapshot = revision.Snapshot,
                Fields = revision.Fields?.Clone(),
                Title = revision.Title,
                Slug = revision.Slug,
                CanonicalPath = revision.CanonicalPath,
                Excerpt = revision.Excerpt,
                SchemaVersion = revision.SchemaVersion,
                CreatedAt = revision.CreatedAt,
                CreatedBy = revision.CreatedBy
            };
            _ = await dbContext.ContentRevisions.AddAsync(persistedRevision, cancellationToken);
            _ = await dbContext.SaveChangesAsync(cancellationToken);

            contentItem.WorkingRevisionId = persistedRevision.Id;
            contentItem.Fields = persistedRevision.Fields?.Clone();
            contentItem.SchemaVersion = persistedRevision.SchemaVersion;
            contentItem.Slug = persistedRevision.Slug ?? contentItem.Slug;
            contentItem.Title = persistedRevision.Title ?? contentItem.Title;
            contentItem.Excerpt = persistedRevision.Excerpt ?? contentItem.Excerpt;
            if (publishSeed)
            {
                contentItem.PublishedRevisionId = persistedRevision.Id;
                contentItem.CanonicalPath = persistedRevision.CanonicalPath!;
                contentItem.Version = 2;
            }

            _ = await dbContext.SaveChangesAsync(cancellationToken);

            if (outboxOperation is not null)
            {
                _ = await dbContext.ContentPublicationOutbox.AddAsync(
                    CreateOutboxRecord(tenantId, contentItem.Id, persistedRevision.Id, outboxOperation.Value),
                    cancellationToken);
                _ = await dbContext.SaveChangesAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return EntityResult<ContentRevision>.Success(persistedRevision);
        }
        catch (DbUpdateConcurrencyException)
        {
            dbContext.ChangeTracker.Clear();
            await transaction.RollbackAsync(cancellationToken);
            return EntityResult<ContentRevision>.Failure(
                EntityError.Concurrency("The content entry was changed by another request."));
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            dbContext.ChangeTracker.Clear();
            await transaction.RollbackAsync(cancellationToken);
            return EntityResult<ContentRevision>.Failure(EntityError.Conflict("A content entry with this path already exists."));
        }
    }

    public async Task<EntityResult> SaveItemAsync(
        string tenantId,
        ContentItem contentItem,
        int expectedVersion,
        ContentPublicationOperation? outboxOperation,
        Guid? revisionId,
        CancellationToken cancellationToken)
    {
        EnsureTenant(tenantId);
        await using IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            ContentItem? storedItem = await ReadItemForUpdateAsync(tenantId, contentItem.Id, cancellationToken);
            if (storedItem is null)
            {
                return await RollbackFailureAsync(transaction, EntityError.NotFound("Content entry was not found."), cancellationToken);
            }

            if (storedItem.Version != expectedVersion)
            {
                dbContext.ChangeTracker.Clear();
                return await RollbackFailureAsync(transaction,
                    EntityError.Concurrency("The content entry was changed by another request."), cancellationToken);
            }

            ContentItem trackedItem = await LoadTrackedItemAsync(tenantId, contentItem.Id, cancellationToken);
            CopyContentItem(contentItem, trackedItem);
            trackedItem.Version = expectedVersion + 1;
            contentItem = trackedItem;
            if (outboxOperation is not null)
            {
                _ = await dbContext.ContentPublicationOutbox.AddAsync(
                    CreateOutboxRecord(tenantId, contentItem.Id, revisionId, outboxOperation.Value),
                    cancellationToken);
            }

            _ = await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return EntityResult.Success();
        }
        catch (DbUpdateConcurrencyException)
        {
            dbContext.ChangeTracker.Clear();
            await transaction.RollbackAsync(cancellationToken);
            return EntityResult.Failure(EntityError.Concurrency("The content entry was changed by another request."));
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            dbContext.ChangeTracker.Clear();
            await transaction.RollbackAsync(cancellationToken);
            return EntityResult.Failure(EntityError.Conflict("A content entry with this path already exists."));
        }
    }

    private async Task<ContentItem?> ReadItemForUpdateAsync(
        string tenantId,
        Guid contentId,
        CancellationToken cancellationToken)
    {
        if (!dbContext.Database.IsRelational())
        {
            return await dbContext.ContentItems.AsNoTracking()
                .SingleOrDefaultAsync(item =>
                    EF.Property<string>(item, nameof(ContentPublicationOutboxRecord.TenantId)) == tenantId && item.Id == contentId,
                    cancellationToken);
        }

        IEntityType entityType = dbContext.Model.FindEntityType(typeof(ContentItem))
            ?? throw new InvalidOperationException("Content item persistence mapping is unavailable.");
        StoreObjectIdentifier table = StoreObjectIdentifier.Table(
            entityType.GetTableName() ?? throw new InvalidOperationException("Content item table mapping is unavailable."),
            entityType.GetSchema());
        ISqlGenerationHelper sql = dbContext.GetService<ISqlGenerationHelper>();
        string tableName = sql.DelimitIdentifier(table.Name, table.Schema);
        string tenantColumn = sql.DelimitIdentifier(entityType.FindProperty(nameof(ContentPublicationOutboxRecord.TenantId))!.GetColumnName(table)!);
        string idColumn = sql.DelimitIdentifier(entityType.FindProperty(nameof(ContentItem.Id))!.GetColumnName(table)!);
        string deletedAtColumn = sql.DelimitIdentifier(entityType.FindProperty(nameof(Entity.DeletedAt))!.GetColumnName(table)!);
        FormattableString query = FormattableStringFactory.Create(
            $"SELECT * FROM {tableName} WHERE {tenantColumn} = {{0}} AND {idColumn} = {{1}} AND {deletedAtColumn} IS NULL FOR UPDATE",
            tenantId,
            contentId);

        return await dbContext.ContentItems.FromSqlInterpolated(query)
            .IgnoreQueryFilters()
            .AsNoTracking()
            .SingleOrDefaultAsync(cancellationToken);
    }

    private async Task<ContentItem> LoadTrackedItemAsync(
        string tenantId,
        Guid contentId,
        CancellationToken cancellationToken)
    {
        ContentItem? trackedItem = await dbContext.ContentItems.SingleOrDefaultAsync(item =>
            EF.Property<string>(item, nameof(ContentPublicationOutboxRecord.TenantId)) == tenantId && item.Id == contentId,
            cancellationToken);
        if (trackedItem is null)
        {
            throw new InvalidOperationException("The locked content entry could not be loaded in the resolved tenant.");
        }

        EntityEntry<ContentItem> entry = dbContext.Entry(trackedItem);
        if (entry.State != EntityState.Added)
        {
            await entry.ReloadAsync(cancellationToken);
        }

        return trackedItem;
    }

    private static void CopyContentItem(ContentItem source, ContentItem destination)
    {
        destination.Type = source.Type;
        destination.Status = source.Status;
        destination.Slug = source.Slug;
        destination.CanonicalPath = source.CanonicalPath;
        destination.Title = source.Title;
        destination.Excerpt = source.Excerpt;
        destination.Body = source.Body;
        destination.Metadata = source.Metadata;
        destination.AuthorId = source.AuthorId;
        destination.Locale = source.Locale;
        destination.PublishedAt = source.PublishedAt;
        destination.SiteId = source.SiteId;
        destination.ContentTypeId = source.ContentTypeId;
        destination.SchemaVersion = source.SchemaVersion;
        destination.Fields = source.Fields?.Clone();
        destination.WorkingRevisionId = source.WorkingRevisionId;
        destination.PublishedRevisionId = source.PublishedRevisionId;
    }

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.GetBaseException() is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };

    private static ContentPublicationOutboxRecord CreateOutboxRecord(
        string tenantId,
        Guid contentId,
        Guid? revisionId,
        ContentPublicationOperation operation)
    {
        DateTime now = DateTime.UtcNow;
        return new ContentPublicationOutboxRecord
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ContentId = contentId,
            RevisionId = revisionId,
            Operation = operation,
            CreatedAt = now,
            Attempts = 0,
            NextAttemptAt = now
        };
    }

    private string EnsureTenant(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(requestContext.TenantId))
        {
            throw new InvalidOperationException("A resolved tenant is required for content access.");
        }

        if (!string.Equals(requestContext.TenantId, tenantId, StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException("The requested tenant does not match the resolved tenant.");
        }

        return tenantId;
    }

    private static async Task<EntityResult<T>> RollbackFailureAsync<T>(
        IDbContextTransaction transaction,
        EntityError error,
        CancellationToken cancellationToken)
    {
        await transaction.RollbackAsync(cancellationToken);
        return EntityResult<T>.Failure(error);
    }

    private static async Task<EntityResult> RollbackFailureAsync(
        IDbContextTransaction transaction,
        EntityError error,
        CancellationToken cancellationToken)
    {
        await transaction.RollbackAsync(cancellationToken);
        return EntityResult.Failure(error);
    }

}
