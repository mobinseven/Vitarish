using System.Linq.Expressions;

namespace Vitarish.Cms.Infrastructure.Repositories;

[RegisterTransient]
internal class TagRepository(CmsDbContext dbContext)
    : EntityRepository<Tag, TagDescriptor, CmsDbContext>(dbContext),
      IEntityRepository<Tag, TagDescriptor>
{
    protected override IQueryable<Tag> Apply(IQueryable<Tag> query, TagDescriptor descriptor)
    {
        query = base.Apply(query, descriptor);

        if (!string.IsNullOrWhiteSpace(descriptor.Search))
        {
            IReadOnlyCollection<string> patterns = EntitySearchHelper.CreateSearchPatterns(descriptor.Search);

            if (patterns.Count > 0)
            {
                Expression<Func<Tag, bool>> predicate = EntitySearchHelper.BuildSearchPredicate<Tag>(
                    patterns,
                    tag => tag.Name,
                    tag => tag.Slug,
                    tag => tag.Description);

                query = query.Where(predicate);
            }
        }

        return query;
    }

    protected override bool IsDuplicateKeyException(DbUpdateException exception) => false;
}
