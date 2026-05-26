using Vitarish.Entities.Server.Data;
namespace Vitarish.Contents.Infrastructure.Repositories;

[RegisterTransient]
internal sealed class ContentAssetRepository(ContentsDbContext dbContext)
    : EntityRepository<ContentAsset, ContentAssetDescriptor, ContentsDbContext>(dbContext),
      IEntityRepository<ContentAsset, ContentAssetDescriptor>
{
    protected override bool IsDuplicateKeyException(DbUpdateException ex)
        => DatabaseExceptions.IsUniqueConstraintViolation(ex);

    protected override IQueryable<ContentAsset> Apply(IQueryable<ContentAsset> query, ContentAssetDescriptor descriptor)
    {
        query = base.Apply(query, descriptor);

        if (descriptor.ContentId is { } contentId)
        {
            query = query.Where(ca => ca.ContentId == contentId);
        }

        if (descriptor.AssetId is { } assetId)
        {
            query = query.Where(ca => ca.AssetId == assetId);
        }

        if (descriptor.Role is { } role)
        {
            query = query.Where(ca => ca.Role == role);
        }

        return query;
    }
}
