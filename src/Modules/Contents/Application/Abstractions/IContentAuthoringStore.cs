using Vitarish.Contents.Application.Models;
using Vitarish.Contents.Domain.Entities;
using Vitarish.Contents.Domain.Enums;
using Vitarish.Entities.Contracts.Models;

namespace Vitarish.Contents.Application.Abstractions;

public interface IContentAuthoringStore
{
    Task<IReadOnlyList<Site>> ListSitesAsync(string tenantId, CancellationToken cancellationToken);
    Task<Site?> GetSiteByIdAsync(string tenantId, Guid siteId, CancellationToken cancellationToken);
    Task<Site?> GetSiteByKeyAsync(string tenantId, string siteKey, CancellationToken cancellationToken);
    Task<EntityResult> AddSiteAsync(string tenantId, Site site, CancellationToken cancellationToken);
    Task<EntityResult> UpdateSiteAsync(string tenantId, Site site, CancellationToken cancellationToken);

    Task<IReadOnlyList<ContentType>> ListContentTypesAsync(string tenantId, CancellationToken cancellationToken);
    Task<ContentType?> GetContentTypeByIdAsync(string tenantId, Guid typeId, CancellationToken cancellationToken);
    Task<ContentType?> GetContentTypeByKeyAsync(string tenantId, string typeKey, CancellationToken cancellationToken);
    Task<ContentTypeSchema?> GetContentTypeSchemaAsync(string tenantId, Guid typeId, int version, CancellationToken cancellationToken);
    Task<EntityResult> AddContentTypeAsync(string tenantId, ContentType contentType, ContentTypeSchema schema, CancellationToken cancellationToken);
    Task<EntityResult> AddContentTypeSchemaAsync(string tenantId, ContentType contentType, ContentTypeSchema schema, CancellationToken cancellationToken);

    Task<IReadOnlyList<ContentItem>> ListEntriesAsync(
        string tenantId,
        Guid siteId,
        Guid? typeId,
        ContentStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken);
    Task<ContentItem?> GetEntryByIdAsync(string tenantId, Guid contentId, CancellationToken cancellationToken);
    Task<IReadOnlyList<ContentRevision>> ListRevisionsAsync(string tenantId, Guid contentId, CancellationToken cancellationToken);
    Task<ContentRevision?> GetRevisionByIdAsync(string tenantId, Guid contentId, Guid revisionId, CancellationToken cancellationToken);

    Task<PublishedContentSnapshot?> GetPublishedEntryByPathAsync(
        string tenantId,
        Guid siteId,
        string path,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<PublishedContentSnapshot>> ListPublishedEntriesAsync(
        string tenantId,
        Guid siteId,
        Guid typeId,
        int skip,
        int take,
        CancellationToken cancellationToken);

    Task<EntityResult<ContentRevision>> SaveRevisionAsync(
        string tenantId,
        ContentItem contentItem,
        ContentRevision revision,
        int? expectedVersion,
        ContentPublicationOperation? outboxOperation,
        CancellationToken cancellationToken);
    Task<EntityResult> SavePublishedSeedAsync(
        string tenantId,
        ContentItem contentItem,
        ContentRevision revision,
        CancellationToken cancellationToken);
    Task<EntityResult> SaveItemAsync(
        string tenantId,
        ContentItem contentItem,
        int expectedVersion,
        ContentPublicationOperation? outboxOperation,
        Guid? revisionId,
        CancellationToken cancellationToken);
}
