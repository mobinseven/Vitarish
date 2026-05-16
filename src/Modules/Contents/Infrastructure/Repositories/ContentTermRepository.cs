namespace Vitarish.Contents.Infrastructure.Repositories;

[RegisterTransient]
internal sealed class ContentTermRepository(ContentsDbContext dbContext)
    : EntityRepository<ContentTerm, ContentTermDescriptor, ContentsDbContext>(dbContext),
      IEntityRepository<ContentTerm, ContentTermDescriptor>
{
    protected override bool IsDuplicateKeyException(DbUpdateException ex)
        => ContentsDatabaseExceptions.IsUniqueConstraintViolation(ex);

    protected override IQueryable<ContentTerm> Apply(IQueryable<ContentTerm> query, ContentTermDescriptor descriptor)
    {
        query = base.Apply(query, descriptor);

        if (descriptor.ContentId is { } contentId)
        {
            query = query.Where(ct => ct.ContentId == contentId);
        }

        if (descriptor.TermId is { } termId)
        {
            query = query.Where(ct => ct.TermId == termId);
        }

        if (descriptor.Role is { } role)
        {
            query = query.Where(ct => ct.Role == role);
        }

        return query;
    }
}
