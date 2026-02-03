using System.Linq.Expressions;

namespace Vitarish.Cms.Infrastructure.Repositories;

[RegisterTransient]
internal class ContentCommentRepository(CmsDbContext dbContext)
    : EntityRepository<ContentComment, ContentCommentDescriptor, CmsDbContext>(dbContext),
      IEntityRepository<ContentComment, ContentCommentDescriptor>
{
    protected override IQueryable<ContentComment> Apply(IQueryable<ContentComment> query, ContentCommentDescriptor descriptor)
    {
        query = base.Apply(query, descriptor);

        if (!string.IsNullOrWhiteSpace(descriptor.Search))
        {
            IReadOnlyCollection<string> patterns = EntitySearchHelper.CreateSearchPatterns(descriptor.Search);

            if (patterns.Count > 0)
            {
                Expression<Func<ContentComment, bool>> predicate = EntitySearchHelper.BuildSearchPredicate<ContentComment>(
                    patterns,
                    comment => comment.AuthorName,
                    comment => comment.Body);

                query = query.Where(predicate);
            }
        }

        if (descriptor.ContentId is not null)
        {
            query = query.Where(comment => comment.ContentId == descriptor.ContentId);
        }

        if (descriptor.IsApproved is not null)
        {
            query = query.Where(comment => comment.IsApproved == descriptor.IsApproved);
        }

        return query;
    }

    protected override bool IsDuplicateKeyException(DbUpdateException exception) => false;
}
