using System.Collections.Immutable;
using System.Text.Json;
using Vitarish.Contents.Application.Abstractions;
using Vitarish.Contents.Application.Models;
using Vitarish.Contents.Domain.Entities;
using Vitarish.Contents.Domain.Enums;
using Vitarish.Contents.Domain.Models;
using Vitarish.Entities.Contracts.Models;
using Vitarish.Entities.Server.Abstractions;
using Vitarish.Entities.Server.Core;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Contents.Application;

public sealed class ContentSeeding(
    IContentAuthoringStore store,
    IRequestContext requestContext,
    IEntityTimeService timeService) : IContentSeeding
{
    private const int EntryPageSize = 100;

    public async Task<EntityResult> EnsureSiteAsync(
        string tenantId,
        string key,
        string name,
        string defaultLocale,
        string[] locales,
        CancellationToken cancellationToken)
    {
        EnsureTenant(tenantId);

        if (await store.GetSiteByKeyAsync(tenantId, key, cancellationToken) is not null)
        {
            return EntityResult.Success();
        }

        Site site = new()
        {
            Id = Guid.NewGuid(),
            Key = key,
            Name = name,
            DefaultLocale = defaultLocale,
            Locales = [.. locales]
        };
        IReadOnlyList<EntityError> errors = site.Validate();
        if (errors.Count > 0)
        {
            return EntityResult.Failure([.. errors]);
        }

        EntityResult result = await store.AddSiteAsync(tenantId, site, cancellationToken);
        if (result.IsSuccess || await store.GetSiteByKeyAsync(tenantId, key, cancellationToken) is not null)
        {
            return EntityResult.Success();
        }

        return result;
    }

    public async Task<EntityResult> EnsureContentTypeAsync(
        string tenantId,
        string key,
        string name,
        IReadOnlyList<ContentFieldDefinition> fields,
        CancellationToken cancellationToken)
    {
        EnsureTenant(tenantId);

        if (await store.GetContentTypeByKeyAsync(tenantId, key, cancellationToken) is not null)
        {
            return EntityResult.Success();
        }

        IReadOnlyList<EntityError> schemaErrors = ContentSchemaRules.Validate(fields);
        if (schemaErrors.Count > 0)
        {
            return EntityResult.Failure([.. schemaErrors]);
        }

        ContentType contentType = new()
        {
            Id = Guid.NewGuid(),
            Key = key,
            Name = name,
            CurrentSchemaVersion = 1
        };
        IReadOnlyList<EntityError> typeErrors = contentType.Validate();
        if (typeErrors.Count > 0)
        {
            return EntityResult.Failure([.. typeErrors]);
        }

        ContentTypeSchema schema = new()
        {
            Id = Guid.NewGuid(),
            ContentTypeId = contentType.Id,
            Version = contentType.CurrentSchemaVersion,
            Fields = fields.ToImmutableArray()
        };
        EntityResult result = await store.AddContentTypeAsync(tenantId, contentType, schema, cancellationToken);
        if (result.IsSuccess || await store.GetContentTypeByKeyAsync(tenantId, key, cancellationToken) is not null)
        {
            return EntityResult.Success();
        }

        return result;
    }

    public async Task<EntityResult> EnsurePublishedEntryAsync(
        string tenantId,
        string siteKey,
        string typeKey,
        string slug,
        string path,
        string title,
        string? excerpt,
        string fieldsJson,
        CancellationToken cancellationToken)
    {
        EnsureTenant(tenantId);

        IReadOnlyList<EntityError> metadataErrors = ValidateEntryMetadata(slug, path, title, excerpt);
        if (metadataErrors.Count > 0)
        {
            return EntityResult.Failure([.. metadataErrors]);
        }

        if (string.IsNullOrWhiteSpace(fieldsJson))
        {
            return EntityResult.Failure(EntityError.Validation("Fields must be a JSON object.", ContentFieldsRules.RootTarget));
        }

        JsonDocument fieldsDocument;
        try
        {
            fieldsDocument = JsonDocument.Parse(fieldsJson);
        }
        catch (JsonException)
        {
            return EntityResult.Failure(EntityError.Validation("Fields must be valid JSON.", ContentFieldsRules.RootTarget));
        }

        using (fieldsDocument)
        {
            JsonElement fields = fieldsDocument.RootElement;
            if (fields.ValueKind != JsonValueKind.Object)
            {
                return EntityResult.Failure(EntityError.Validation("Fields must be a JSON object.", ContentFieldsRules.RootTarget));
            }

            Site? site = await store.GetSiteByKeyAsync(tenantId, siteKey, cancellationToken);
            if (site is null)
            {
                return EntityResult.Failure(EntityError.NotFound("Site was not found."));
            }

            ContentType? contentType = await store.GetContentTypeByKeyAsync(tenantId, typeKey, cancellationToken);
            if (contentType is null)
            {
                return EntityResult.Failure(EntityError.NotFound("Content type was not found."));
            }

            if (await EntryExistsAsync(tenantId, site.Id, slug, path, cancellationToken))
            {
                return EntityResult.Success();
            }

            ContentTypeSchema? schema = await store.GetContentTypeSchemaAsync(
                tenantId,
                contentType.Id,
                contentType.CurrentSchemaVersion,
                cancellationToken);
            if (schema is null)
            {
                return EntityResult.Failure(EntityError.Unknown("The current content type schema could not be loaded."));
            }

            IReadOnlyList<EntityError> fieldErrors = await ContentFieldsValidation.ValidateAsync(
                store,
                tenantId,
                schema.Fields,
                fields,
                site.Locales,
                ContentFieldsValidationMode.Publish,
                cancellationToken);
            if (fieldErrors.Count > 0)
            {
                return EntityResult.Failure([.. fieldErrors]);
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
            ContentRevision revision = new()
            {
                Id = Guid.NewGuid(),
                ContentId = item.Id,
                Fields = fields.Clone(),
                Title = title,
                Slug = slug,
                CanonicalPath = path,
                Excerpt = excerpt ?? string.Empty,
                SchemaVersion = contentType.CurrentSchemaVersion,
                CreatedBy = requestContext.UserId
            };
            item.SaveDraft(revision);
            try
            {
                item.Publish(revision, timeService.Now);
            }
            catch (EntityFailureException exception)
            {
                return EntityResult.Failure(exception.Error);
            }

            item.CanonicalPath = string.Empty;
            item.Version = 1;

            return await store.SavePublishedSeedAsync(
                tenantId,
                item,
                revision,
                cancellationToken);
        }
    }

    private async Task<bool> EntryExistsAsync(
        string tenantId,
        Guid siteId,
        string slug,
        string path,
        CancellationToken cancellationToken)
    {
        for (int skip = 0; ; skip = checked(skip + EntryPageSize))
        {
            IReadOnlyList<ContentItem> entries = await store.ListEntriesAsync(
                tenantId,
                siteId,
                typeId: null,
                status: null,
                skip,
                EntryPageSize,
                cancellationToken);

            foreach (ContentItem entry in entries)
            {
                if (Matches(entry.Slug, slug) || Matches(entry.CanonicalPath, path))
                {
                    return true;
                }

                IReadOnlyList<ContentRevision> revisions = await store.ListRevisionsAsync(tenantId, entry.Id, cancellationToken);
                if (revisions.Any(revision => Matches(revision.Slug, slug) || Matches(revision.CanonicalPath, path)))
                {
                    return true;
                }
            }

            if (entries.Count < EntryPageSize)
            {
                return false;
            }
        }
    }

    private static IReadOnlyList<EntityError> ValidateEntryMetadata(string slug, string path, string title, string? excerpt)
    {
        List<EntityError> errors = [];
        if (string.IsNullOrWhiteSpace(slug) || slug.Length > 256)
        {
            errors.Add(EntityError.Validation("Slug is required and must not exceed 256 characters.", nameof(slug)));
        }

        if (string.IsNullOrWhiteSpace(path) || path.Length > 512)
        {
            errors.Add(EntityError.Validation("Canonical path is required and must not exceed 512 characters.", nameof(path)));
        }

        if (string.IsNullOrWhiteSpace(title) || title.Length > 512)
        {
            errors.Add(EntityError.Validation("Title is required and must not exceed 512 characters.", nameof(title)));
        }

        if (excerpt?.Length > 4000)
        {
            errors.Add(EntityError.Validation("Excerpt is too long.", nameof(excerpt)));
        }

        return errors;
    }

    private static bool Matches(string? existing, string expected) =>
        string.Equals(existing, expected, StringComparison.Ordinal);

    private void EnsureTenant(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(requestContext.TenantId))
        {
            throw new InvalidOperationException("A resolved tenant is required for content seeding.");
        }

        if (!string.Equals(requestContext.TenantId, tenantId, StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException("The requested tenant does not match the resolved tenant.");
        }
    }
}
