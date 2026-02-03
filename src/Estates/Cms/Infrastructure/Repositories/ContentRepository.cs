using System.Linq.Expressions;

namespace Vitarish.Cms.Infrastructure.Repositories;

[RegisterTransient]
internal class ContentRepository(CmsDbContext dbContext)
    : EntityRepository<ContentItem, ContentDescriptor, CmsDbContext>(dbContext),
      IEntityRepository<ContentItem, ContentDescriptor>
{
    protected override IQueryable<ContentItem> Apply(IQueryable<ContentItem> query, ContentDescriptor descriptor)
    {
        query = base.Apply(query, descriptor);

        if (!string.IsNullOrWhiteSpace(descriptor.Search))
        {
            IReadOnlyCollection<string> patterns = EntitySearchHelper.CreateSearchPatterns(descriptor.Search);

            if (patterns.Count > 0)
            {
                Expression<Func<ContentItem, bool>> predicate = EntitySearchHelper.BuildSearchPredicate<ContentItem>(
                    patterns,
                    content => content.Title,
                    content => content.Slug,
                    content => content.Summary,
                    content => content.BodyHtml);

                query = query.Where(predicate);
            }
        }

        if (descriptor.Status is not null)
        {
            query = query.Where(content => content.Status == descriptor.Status);
        }

        if (descriptor.TagId is not null)
        {
            query = query.Where(content => content.Tags.Any(tag => tag.TagId == descriptor.TagId));
        }

        if (descriptor.CategoryId is not null)
        {
            query = query.Where(content => content.Categories.Any(category => category.CategoryId == descriptor.CategoryId));
        }

        return query;
    }

    protected override bool IsDuplicateKeyException(DbUpdateException exception) => false;
}
