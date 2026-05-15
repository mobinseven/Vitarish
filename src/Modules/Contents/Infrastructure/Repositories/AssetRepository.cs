namespace Vitarish.Contents.Infrastructure.Repositories;

[RegisterTransient]
internal sealed class AssetRepository(ContentsDbContext dbContext)
    : EntityRepository<Asset, AssetDescriptor, ContentsDbContext>(dbContext),
      IEntityRepository<Asset, AssetDescriptor>
{
    protected override bool IsDuplicateKeyException(DbUpdateException ex)
        => ex.InnerException?.Message?.Contains("duplicate key", StringComparison.OrdinalIgnoreCase) == true
           || ex.InnerException?.Message?.Contains("unique", StringComparison.OrdinalIgnoreCase) == true;

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
