using System.Linq.Expressions;

namespace Vitarish.Sea.Infrastructure.Repositories;

[RegisterTransient]
internal class RoleRepository(SeaDbContext dbContext)
    : EntityRepository<Role, RoleDescriptor, SeaDbContext>(dbContext),
      IEntityRepository<Role, RoleDescriptor>
{
    private static readonly EntitySearchTextNormalizer SearchTextNormalizer = new();
    private static readonly EntitySearchPatternFactory SearchPatternFactory = new(SearchTextNormalizer);
    private static readonly EntitySearchPredicateBuilder SearchPredicateBuilder = new(SearchTextNormalizer);

    protected override IQueryable<Role> Apply(IQueryable<Role> query, RoleDescriptor descriptor)
    {
        query = base.Apply(query, descriptor);

        if (!string.IsNullOrWhiteSpace(descriptor.Search))
        {
            IReadOnlyCollection<string> patterns = SearchPatternFactory.CreateSearchPatterns(descriptor.Search);

            if (patterns.Count > 0)
            {
                Expression<Func<Role, bool>> predicate = SearchPredicateBuilder.BuildSearchPredicate<Role>(
                    patterns,
                    role => role.Name);

                query = query.Where(predicate);
            }
        }

        return query;
    }

    protected override bool IsDuplicateKeyException(DbUpdateException exception) => false;
}

