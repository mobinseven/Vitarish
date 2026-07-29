using Vitarish.Entities.Contracts.Models;
using Vitarish.Contents.Application.Abstractions;
using Vitarish.Contents.Domain.Enums;

namespace Vitarish.Contents.Application;

public sealed class ContentManagementService(
    IContentItemsService contentItems,
    IContentRevisionsService contentRevisions) : IContentManagementService
{
    public Task<EntityResult<IReadOnlyList<ContentItemReadDto>>> ListAsync(CancellationToken cancellationToken) =>
        contentItems.GetListAsync(new ContentItemDescriptorDto(), cancellationToken);

    public Task<EntityResult<ContentItemReadDto>> GetAsync(Guid contentId, CancellationToken cancellationToken) =>
        contentItems.GetAsync(contentId, cancellationToken);

    public Task<EntityResult<ContentItemReadDto>> CreateAsync(
        ContentItemWriteDto item,
        CancellationToken cancellationToken) => contentItems.AddAsync(item, cancellationToken);

    public async Task<EntityResult<ContentItemReadDto>> UpdateAsync(
        Guid contentId,
        ContentItemWriteDto item,
        CancellationToken cancellationToken)
    {
        EntityResult<ContentItemReadDto> currentResult = await contentItems.GetAsync(contentId, cancellationToken);
        if (currentResult.IsFailure || currentResult.Value is null)
        {
            return EntityResult<ContentItemReadDto>.Failure([.. currentResult.Errors]);
        }

        ContentItemReadDto current = currentResult.Value;
        item.Id = contentId;
        item.AuthorId = current.AuthorId;
        item.Metadata = current.Metadata;
        item.PublishedAt = item.Status == ContentStatus.Published ? current.PublishedAt : null;
        return await contentItems.UpdateAsync(item, cancellationToken);
    }

    public async Task<EntityResult<ContentItemReadDto>> ArchiveAsync(
        Guid contentId,
        CancellationToken cancellationToken)
    {
        EntityResult<ContentItemReadDto> currentResult = await contentItems.GetAsync(contentId, cancellationToken);
        if (currentResult.IsFailure || currentResult.Value is null)
        {
            return EntityResult<ContentItemReadDto>.Failure([.. currentResult.Errors]);
        }

        ContentItemReadDto current = currentResult.Value;
        ContentItemWriteDto archived = new()
        {
            Id = current.Id,
            Type = current.Type,
            Status = ContentStatus.Archived,
            Slug = current.Slug,
            CanonicalPath = current.CanonicalPath,
            Title = current.Title,
            Excerpt = current.Excerpt,
            Body = current.Body,
            Metadata = current.Metadata,
            AuthorId = current.AuthorId,
            Locale = current.Locale,
            PublishedAt = current.PublishedAt
        };
        return await contentItems.UpdateAsync(archived, cancellationToken);
    }

    public Task<EntityResult<IReadOnlyList<ContentRevisionReadDto>>> ListRevisionsAsync(
        Guid contentId,
        CancellationToken cancellationToken) =>
        contentRevisions.GetListAsync(new ContentRevisionDescriptorDto { ContentId = contentId }, cancellationToken);
}
