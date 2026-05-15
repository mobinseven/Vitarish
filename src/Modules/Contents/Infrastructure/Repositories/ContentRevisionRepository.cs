namespace Vitarish.Contents.Infrastructure.Repositories;

[RegisterTransient]
internal sealed class ContentRevisionRepository(ContentsDbContext dbContext)
    : EntityRepository<ContentRevision, ContentRevisionDescriptor, ContentsDbContext>(dbContext),
      IEntityRepository<ContentRevision, ContentRevisionDescriptor>
{
    protected override bool IsDuplicateKeyException(DbUpdateException ex)
        => ex.InnerException?.Message?.Contains("duplicate key", StringComparison.OrdinalIgnoreCase) == true
           || ex.InnerException?.Message?.Contains("unique", StringComparison.OrdinalIgnoreCase) == true;

    protected override IQueryable<ContentRevision> Apply(IQueryable<ContentRevision> query, ContentRevisionDescriptor descriptor)
    {
        query = base.Apply(query, descriptor);

        if (descriptor.ContentId is { } contentId)
        {
            query = query.Where(cr => cr.ContentId == contentId);
        }

        if (descriptor.Version is { } version)
        {
            query = query.Where(cr => cr.Version == version);
        }

        return query.OrderByDescending(cr => cr.Version);
    }
}
