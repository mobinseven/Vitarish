using Vitarish.Contents.Domain.Models;
using Vitarish.Entities.Contracts.Models;

namespace Vitarish.Contents.Application.Abstractions;

public interface IContentSeeding
{
    Task<EntityResult> EnsureSiteAsync(
        string tenantId,
        string key,
        string name,
        string defaultLocale,
        string[] locales,
        CancellationToken cancellationToken);

    Task<EntityResult> EnsureContentTypeAsync(
        string tenantId,
        string key,
        string name,
        IReadOnlyList<ContentFieldDefinition> fields,
        CancellationToken cancellationToken);

    Task<EntityResult> EnsurePublishedEntryAsync(
        string tenantId,
        string siteKey,
        string typeKey,
        string slug,
        string path,
        string title,
        string? excerpt,
        string fieldsJson,
        CancellationToken cancellationToken);
}
