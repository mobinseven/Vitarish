using Microsoft.EntityFrameworkCore;
using Vitarish.Clinical.Domain.Abstractions;
using Vitarish.Clinical.Domain.Entities;
using Vitarish.Clinical.Domain.Models;
using Vitarish.Clinical.Infrastructure.Data;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Clinical.Infrastructure.Services;

internal sealed class ClinicalResourceLinkService(
    ClinicalDbContext dbContext,
    IRequestContext requestContext) : IClinicalResourceLinkService
{
    public async Task<ResourceLinkReadDto> LinkAsync(ResourceLinkWriteDto writeDto, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(writeDto.LocalEntityType);
        ArgumentException.ThrowIfNullOrWhiteSpace(writeDto.ResourceType);
        ArgumentException.ThrowIfNullOrWhiteSpace(writeDto.ResourceId);

        string localEntityType = writeDto.LocalEntityType.Trim();
        string resourceType = writeDto.ResourceType.Trim();
        string resourceId = writeDto.ResourceId.Trim();

        ClinicalResourceLink? existing = await dbContext.ResourceLinks
            .FirstOrDefaultAsync(link =>
                link.LocalEntityType == localEntityType &&
                link.LocalEntityId == writeDto.LocalEntityId &&
                link.ResourceType == resourceType &&
                link.ResourceId == resourceId,
                cancellationToken);

        if (existing is not null)
        {
            existing.IsArchived = false;
            existing.ArchivedAt = null;
            existing.ResourceVersionId = writeDto.ResourceVersionId?.Trim();
            existing.OwnerUserId = writeDto.OwnerUserId?.Trim();
            existing.PatientReference = writeDto.PatientReference?.Trim();
            await dbContext.SaveChangesAsync(cancellationToken);
            return Map(existing);
        }

        ClinicalResourceLink link = new()
        {
            IsArchived = false,
            LocalEntityId = writeDto.LocalEntityId,
            LocalEntityType = localEntityType,
            OwnerUserId = writeDto.OwnerUserId?.Trim() ?? requestContext.UserId,
            PatientReference = writeDto.PatientReference?.Trim(),
            ResourceId = resourceId,
            ResourceType = resourceType,
            ResourceVersionId = writeDto.ResourceVersionId?.Trim()
        };

        dbContext.ResourceLinks.Add(link);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Map(link);
    }

    public async Task UnlinkAsync(Guid id, CancellationToken cancellationToken = default)
    {
        ClinicalResourceLink? link = await dbContext.ResourceLinks
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken)
            ?? throw new InvalidOperationException($"Resource link '{id}' was not found.");

        link.IsArchived = true;
        link.ArchivedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ResourceLinkReadDto>> GetLinksAsync(ResourceLinkFilter filter, CancellationToken cancellationToken = default)
    {
        IQueryable<ClinicalResourceLink> query = dbContext.ResourceLinks.AsNoTracking();

        if (!filter.IncludeArchived)
        {
            query = query.Where(link => !link.IsArchived);
        }

        if (!string.IsNullOrWhiteSpace(filter.LocalEntityType))
        {
            string localEntityType = filter.LocalEntityType.Trim();
            query = query.Where(link => link.LocalEntityType == localEntityType);
        }

        if (filter.LocalEntityId is not null)
        {
            query = query.Where(link => link.LocalEntityId == filter.LocalEntityId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.ResourceType))
        {
            string resourceType = filter.ResourceType.Trim();
            query = query.Where(link => link.ResourceType == resourceType);
        }

        if (!string.IsNullOrWhiteSpace(filter.ResourceId))
        {
            string resourceId = filter.ResourceId.Trim();
            query = query.Where(link => link.ResourceId == resourceId);
        }

        if (!string.IsNullOrWhiteSpace(filter.SearchText))
        {
            string searchText = filter.SearchText.Trim();
            query = query.Where(link =>
                link.ResourceId.Contains(searchText) ||
                link.ResourceType.Contains(searchText) ||
                (link.PatientReference != null && link.PatientReference.Contains(searchText)));
        }

        int take = filter.Take <= 0 ? 50 : Math.Min(filter.Take, 200);

        return await query
            .OrderByDescending(link => link.CreatedAt)
            .Take(take)
            .Select(link => new ResourceLinkReadDto
            {
                Id = link.Id,
                ArchivedAt = link.ArchivedAt,
                CreatedAt = link.CreatedAt,
                ETag = link.ETag,
                IsArchived = link.IsArchived,
                LocalEntityId = link.LocalEntityId,
                LocalEntityType = link.LocalEntityType,
                OwnerUserId = link.OwnerUserId,
                PatientReference = link.PatientReference,
                ResourceId = link.ResourceId,
                ResourceType = link.ResourceType,
                ResourceVersionId = link.ResourceVersionId
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ResourceLinkReadDto?> GetLinkAsync(Guid id, CancellationToken cancellationToken = default)
    {
        ClinicalResourceLink? link = await dbContext.ResourceLinks
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

        return link is null ? null : Map(link);
    }

    private static ResourceLinkReadDto Map(ClinicalResourceLink link) => new()
    {
        Id = link.Id,
        ArchivedAt = link.ArchivedAt,
        CreatedAt = link.CreatedAt,
        ETag = link.ETag,
        IsArchived = link.IsArchived,
        LocalEntityId = link.LocalEntityId,
        LocalEntityType = link.LocalEntityType,
        OwnerUserId = link.OwnerUserId,
        PatientReference = link.PatientReference,
        ResourceId = link.ResourceId,
        ResourceType = link.ResourceType,
        ResourceVersionId = link.ResourceVersionId
    };
}
