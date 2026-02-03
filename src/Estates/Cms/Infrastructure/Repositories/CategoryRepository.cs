using System.Linq.Expressions;

namespace Vitarish.Cms.Infrastructure.Repositories;

[RegisterTransient]
internal class CategoryRepository(CmsDbContext dbContext)
    : EntityRepository<Category, CategoryDescriptor, CmsDbContext>(dbContext),
      IEntityRepository<Category, CategoryDescriptor>
{
    protected override IQueryable<Category> Apply(IQueryable<Category> query, CategoryDescriptor descriptor)
    {
        query = base.Apply(query, descriptor);

        if (!string.IsNullOrWhiteSpace(descriptor.Search))
        {
            IReadOnlyCollection<string> patterns = EntitySearchHelper.CreateSearchPatterns(descriptor.Search);

            if (patterns.Count > 0)
            {
                Expression<Func<Category, bool>> predicate = EntitySearchHelper.BuildSearchPredicate<Category>(
                    patterns,
                    category => category.Name,
                    category => category.Slug);

                query = query.Where(predicate);
            }
        }

        return query;
    }

    protected override bool IsDuplicateKeyException(DbUpdateException exception) => false;
}
