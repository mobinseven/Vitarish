using Microsoft.EntityFrameworkCore.Storage;
using Vitarish.Contents.Domain.Policies;
using Vitarish.Contents.Infrastructure.SearchProviders;
using Vitarish.Entities.Contracts.Models;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Search.Application.Abstractions;
using Vitarish.Search.Domain.Models;

namespace Vitarish.Contents.Infrastructure.Services;

[RegisterTransient]
internal sealed class ContentItemsService(
    IMediator mediator,
    ContentsDbContext dbContext,
    IRequestContext requestContext,
    ISearchService? searchService = null)
    : EntityService<ContentItem, ContentItemDescriptor, ContentItemReadDto, ContentItemWriteDto, ContentItemDescriptorDto>(
        mediator,
        ContentsEntityMapperExtensions.ToRead,
        ContentsEntityMapperExtensions.ToEntity,
        ContentsEntityMapperExtensions.ToDescriptor),
      IContentItemsService
{
    public new async Task<EntityResult<ContentItemReadDto>> AddAsync(ContentItemWriteDto dto, CancellationToken cancellationToken)
    {
        EntityResult<ContentItemReadDto> result = await ExecuteInContentTransactionAsync(
            async () =>
            {
                EntityResult<ContentItemReadDto> addResult = await base.AddAsync(dto, cancellationToken);
                if (addResult.IsFailure || addResult.Value is null)
                {
                    return addResult;
                }

                if (ContentItemRules.ShouldCreateRevisionOnCreate(dto.Status))
                {
                    await CreateRevisionAsync(addResult.Value.Id, dto, cancellationToken);
                }

                return addResult;
            },
            cancellationToken);

        if (result.IsFailure || result.Value is null)
        {
            return result;
        }

        await TryIndexContentAsync(result.Value.Id, dto, cancellationToken);

        return result;
    }

    public new async Task<EntityResult<ContentItemReadDto>> UpdateAsync(ContentItemWriteDto dto, CancellationToken cancellationToken)
    {
        var existingItem = await dbContext.ContentItems
            .AsNoTracking()
            .Where(c => c.Id == dto.Id)
            .Select(c => new { c.Status, c.SiteId })
            .FirstOrDefaultAsync(cancellationToken);

        if (existingItem?.SiteId is not null)
        {
            return EntityResult<ContentItemReadDto>.Failure(
                ContentItemRules.StructuredMutationConflict());
        }

        ContentStatus? previousStatus = existingItem is null ? null : existingItem.Status;

        EntityResult<ContentItemReadDto> result = await ExecuteInContentTransactionAsync(
            async () =>
            {
                EntityResult<ContentItemReadDto> updateResult = await base.UpdateAsync(dto, cancellationToken);
                if (updateResult.IsFailure || updateResult.Value is null)
                {
                    return updateResult;
                }

                if (ContentItemRules.ShouldCreateRevisionOnUpdate(previousStatus, dto.Status))
                {
                    await CreateRevisionAsync(dto.Id, dto, cancellationToken);
                }

                return updateResult;
            },
            cancellationToken);

        if (result.IsFailure || result.Value is null)
        {
            return result;
        }

        await TryIndexContentAsync(result.Value.Id, dto, cancellationToken);

        return result;
    }

    private async Task<EntityResult<ContentItemReadDto>> ExecuteInContentTransactionAsync(
        Func<Task<EntityResult<ContentItemReadDto>>> operation,
        CancellationToken cancellationToken)
    {
        if (dbContext.Database.CurrentTransaction is not null)
        {
            return await operation();
        }

        await using IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        EntityResult<ContentItemReadDto> result = await operation();

        if (result.IsFailure)
        {
            await transaction.RollbackAsync(cancellationToken);
            return result;
        }

        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    private async Task CreateRevisionAsync(Guid contentId, ContentItemWriteDto dto, CancellationToken cancellationToken)
    {
        int nextVersion = await dbContext.ContentRevisions
            .Where(r => r.ContentId == contentId)
            .Select(r => r.Version)
            .DefaultIfEmpty(0)
            .MaxAsync(cancellationToken);

        string snapshot = JsonSerializer.Serialize(new
        {
            dto.Type,
            dto.Status,
            dto.Slug,
            dto.CanonicalPath,
            dto.Title,
            dto.Excerpt,
            dto.Body,
            dto.Metadata,
            dto.AuthorId,
            dto.Locale,
            dto.PublishedAt
        }, JsonSerializerOptions.Web);

        ContentRevision revision = new()
        {
            Id = Guid.NewGuid(),
            ContentId = contentId,
            Version = nextVersion + 1,
            Snapshot = snapshot
        };

        _ = await dbContext.ContentRevisions.AddAsync(revision, cancellationToken);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task TryIndexContentAsync(Guid contentId, ContentItemWriteDto dto, CancellationToken cancellationToken)
    {
        if (searchService is null)
        {
            return;
        }

        string tenantId = !string.IsNullOrWhiteSpace(requestContext.TenantId)
            ? requestContext.TenantId
            : throw new InvalidOperationException("Tenant ID must be set before indexing content documents.");

        if (ContentSearchVisibility.IsSearchable(dto.Status))
        {
            await searchService.IndexAsync(
                ContentItemsSearchProvider.IndexNameValue,
                [ContentItemsSearchProvider.CreateDocument(contentId, dto, tenantId)],
                cancellationToken);
        }
        else
        {
            await searchService.DeleteAsync(
                ContentItemsSearchProvider.IndexNameValue,
                new SearchDeleteOptions
                {
                    TenantId = tenantId,
                    MetadataFilters = new Dictionary<string, string>
                    {
                        [ContentItemsSearchProvider.ContentIdMetadataKey] = contentId.ToString()
                    }
                },
                cancellationToken);
        }
    }
}
