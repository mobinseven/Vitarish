using Vitarish.Entities.Server.Data;
namespace Vitarish.Contents.Infrastructure.Repositories;

[RegisterTransient]
internal sealed class AssetRepository(ContentsDbContext dbContext)
    : EntityRepository<Asset, AssetDescriptor, ContentsDbContext>(dbContext),
      IEntityRepository<Asset, AssetDescriptor>
{
    protected override bool IsDuplicateKeyException(DbUpdateException ex)
        => DatabaseExceptions.IsUniqueConstraintViolation(ex);

    protected override IQueryable<Asset> Apply(IQueryable<Asset> query, AssetDescriptor descriptor)
    {
        query = base.Apply(query, descriptor);

        if (descriptor.Type is { } type)
        {
            query = query.Where(a => a.Type == type);
        }

        if (descriptor.FileId is { } fileId)
        {
            query = query.Where(a => a.FileId == fileId);
        }

        if (!string.IsNullOrWhiteSpace(descriptor.Url))
        {
            string url = descriptor.Url.Trim();
            query = query.Where(a => a.Url == url);
        }

        return query;
    }
}
