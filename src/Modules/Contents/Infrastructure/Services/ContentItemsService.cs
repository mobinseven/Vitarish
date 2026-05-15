using Vitarish.Search.Domain.Abstractions;
using Vitarish.Contents.Infrastructure.SearchProviders;

namespace Vitarish.Contents.Infrastructure.Services;

[RegisterTransient]
internal sealed class ContentItemsService(
    IMediator mediator,
    ContentsDbContext dbContext,
    IEntityTimeService timeService,
    ISearchService? searchService = null)
    : EntityService<ContentItem, ContentItemDescriptor, ContentItemReadDto, ContentItemWriteDto, ContentItemDescriptorDto>(
        mediator,
        ContentsEntityMapperExtensions.ToDto,
        ContentsEntityMapperExtensions.ToDomain,
        ContentsEntityMapperExtensions.ToDescriptor),
      IContentItemsService
{
    private static readonly HashSet<ContentStatus> RevisionStatuses =
    [
        ContentStatus.Published,
        ContentStatus.Archived
    ];

    public new async Task<EntityResult<ContentItemReadDto>> AddAsync(ContentItemWriteDto dto, CancellationToken cancellationToken)
    {
        EntityResult validation = Validate(dto);
        if (validation.IsFailure)
        {
            return EntityResult<ContentItemReadDto>.Failure([.. validation.Errors]);
        }

        if (dto.Status == ContentStatus.Published && dto.PublishedAt is null)
        {
            dto.PublishedAt = timeService.Now;
        }

        EntityResult<ContentItemReadDto> result = await base.AddAsync(dto, cancellationToken);
        if (result.IsFailure || result.Value is null)
        {
            return result;
        }

        if (ShouldCreateRevision(dto.Status))
        {
            await CreateRevisionAsync(result.Value.Id, dto, cancellationToken);
        }

        await TryReindexAsync(cancellationToken);

        return result;
    }

    public new async Task<EntityResult<ContentItemReadDto>> UpdateAsync(ContentItemWriteDto dto, CancellationToken cancellationToken)
    {
        EntityResult validation = Validate(dto);
        if (validation.IsFailure)
        {
            return EntityResult<ContentItemReadDto>.Failure([.. validation.Errors]);
        }

        ContentStatus? previousStatus = await dbContext.ContentItems
            .AsNoTracking()
            .Where(c => c.Id == dto.Id)
            .Select(c => (ContentStatus?)c.Status)
            .FirstOrDefaultAsync(cancellationToken);

        if (dto.Status == ContentStatus.Published && dto.PublishedAt is null)
        {
            dto.PublishedAt = timeService.Now;
        }

        EntityResult<ContentItemReadDto> result = await base.UpdateAsync(dto, cancellationToken);
        if (result.IsFailure || result.Value is null)
        {
            return result;
        }

        if (previousStatus is not null && previousStatus != dto.Status && ShouldCreateRevision(dto.Status))
        {
            await CreateRevisionAsync(dto.Id, dto, cancellationToken);
        }

        await TryReindexAsync(cancellationToken);

        return result;
    }

    private static bool ShouldCreateRevision(ContentStatus status) => RevisionStatuses.Contains(status);

    private async Task CreateRevisionAsync(Guid contentId, ContentItemWriteDto dto, CancellationToken cancellationToken)
    {
        int nextVersion = await dbContext.ContentRevisions
            .Where(r => r.ContentId == contentId)
            .Select(r => (int?)r.Version)
            .MaxAsync(cancellationToken) ?? 0;

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
        });

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

    private static EntityResult Validate(ContentItemWriteDto dto)
    {
        List<EntityError> errors = [];

        if (string.IsNullOrWhiteSpace(dto.Slug))
        {
            errors.Add(EntityError.Validation("Slug is required.", nameof(dto.Slug)));
        }

        if (string.IsNullOrWhiteSpace(dto.CanonicalPath))
        {
            errors.Add(EntityError.Validation("Canonical path is required.", nameof(dto.CanonicalPath)));
        }

        if (string.IsNullOrWhiteSpace(dto.Title))
        {
            errors.Add(EntityError.Validation("Title is required.", nameof(dto.Title)));
        }

        if (string.IsNullOrWhiteSpace(dto.Locale))
        {
            errors.Add(EntityError.Validation("Locale is required.", nameof(dto.Locale)));
        }

        if (dto.Status == ContentStatus.Scheduled && dto.PublishedAt is null)
        {
            errors.Add(EntityError.Validation("Scheduled content requires PublishedAt.", nameof(dto.PublishedAt)));
        }

        return errors.Count == 0 ? EntityResult.Success() : EntityResult.Failure([.. errors]);
    }

    private async Task TryReindexAsync(CancellationToken cancellationToken)
    {
        if (searchService is null)
        {
            return;
        }

        await searchService.RebuildIndexAsync(ContentItemsSearchProvider.IndexNameValue, cancellationToken);
    }
}
