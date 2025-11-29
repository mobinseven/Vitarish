namespace Vorn.Files.Core.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using System.Linq;
using Vorn.Entities;
using Vorn.Files.Core.Domain.Models;
using Vorn.Files.Core.Infrastructure.Data;

[RegisterTransient]
internal sealed class FileInformationRepository(FilesDbContext dbContext)
    : EntityRepository<FileInformation, FileInformationDescriptor, FilesDbContext>(dbContext), IEntityRepository<FileInformation, FileInformationDescriptor>
{
    protected override IQueryable<FileInformation> Apply(IQueryable<FileInformation> query, FileInformationDescriptor descriptor)
    {
        base.Apply(query, descriptor);

        if (!string.IsNullOrWhiteSpace(descriptor.Url))
        {
            query = query.Where(entity => entity.Url == descriptor.Url);
        }

        if (!string.IsNullOrWhiteSpace(descriptor.Checksum))
        {
            query = query.Where(entity => entity.Checksum == descriptor.Checksum);
        }

        if (!string.IsNullOrWhiteSpace(descriptor.Owner))
        {
            string owner = descriptor.Owner;
            query = query.Where(entity => entity.TenantId != null && entity.TenantId == owner);
        }

        if (!string.IsNullOrWhiteSpace(descriptor.Tag))
        {
            string tag = descriptor.Tag;
            query = query.Where(entity => entity.Tag != null && EF.Functions.Like(entity.Tag, $"%{tag}%"));
        }

        if (!string.IsNullOrWhiteSpace(descriptor.Name))
        {
            string name = descriptor.Name;
            query = query.Where(entity => entity.Url.EndsWith(name));
        }

        if (!string.IsNullOrWhiteSpace(descriptor.Search))
        {
            string search = descriptor.Search;
            query = query.Where(entity => EF.Functions.Like(entity.Url, $"%{search}%") || entity.TenantId != null && EF.Functions.Like(entity.TenantId, $"%{search}%"));
        }

        if (descriptor.CreatedFrom is { } createdFrom)
        {
            query = query.Where(entity => entity.CreatedAt >= createdFrom);
        }

        if (descriptor.CreatedTo is { } createdTo)
        {
            query = query.Where(entity => entity.CreatedAt <= createdTo);
        }

        if (descriptor.Skip is { } skip)
        {
            query = query.Skip(skip);
        }

        if (descriptor.Take is { } take)
        {
            query = query.Take(take);
        }
        return query;
    }
}
