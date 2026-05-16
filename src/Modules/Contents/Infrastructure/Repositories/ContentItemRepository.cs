namespace Vitarish.Contents.Infrastructure.Repositories;

[RegisterTransient]
internal sealed class ContentItemRepository(ContentsDbContext dbContext)
    : EntityRepository<ContentItem, ContentItemDescriptor, ContentsDbContext>(dbContext),
      IEntityRepository<ContentItem, ContentItemDescriptor>
{
    protected override bool IsDuplicateKeyException(DbUpdateException ex)
        => ContentsDatabaseExceptions.IsUniqueConstraintViolation(ex);

    protected override IQueryable<ContentItem> Apply(IQueryable<ContentItem> query, ContentItemDescriptor descriptor)
    {
        query = base.Apply(query, descriptor);

        if (descriptor.Type is { } type)
        {
            query = query.Where(c => c.Type == type);
        }

        if (descriptor.Status is { } status)
        {
            query = query.Where(c => c.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(descriptor.Slug))
        {
            string slug = descriptor.Slug.Trim();
            query = query.Where(c => c.Slug == slug);
        }

        if (!string.IsNullOrWhiteSpace(descriptor.CanonicalPath))
        {
            string canonicalPath = descriptor.CanonicalPath.Trim();
            query = query.Where(c => c.CanonicalPath == canonicalPath);
        }

        if (descriptor.AuthorId is { } authorId)
        {
            query = query.Where(c => c.AuthorId == authorId);
        }

        if (!string.IsNullOrWhiteSpace(descriptor.Locale))
        {
            string locale = descriptor.Locale.Trim();
            query = query.Where(c => c.Locale == locale);
        }

        if (descriptor.PublishedFrom is { } publishedFrom)
        {
            query = query.Where(c => c.PublishedAt != null && c.PublishedAt >= publishedFrom);
        }

        if (descriptor.PublishedTo is { } publishedTo)
        {
            query = query.Where(c => c.PublishedAt != null && c.PublishedAt <= publishedTo);
        }

        if (descriptor.TermId is { } termId)
        {
            query = query.Where(c => dbContext.ContentTerms.Any(ct => ct.ContentId == c.Id && ct.TermId == termId));
        }

        if (descriptor.TermIds is { Count: > 0 } termIds)
        {
            query = query.Where(c => dbContext.ContentTerms.Any(ct => ct.ContentId == c.Id && termIds.Contains(ct.TermId)));
        }

        if (!string.IsNullOrWhiteSpace(descriptor.Search))
        {
            string search = descriptor.Search.Trim();
            query = query.Where(c =>
                EF.Functions.Like(c.Title, $"%{search}%") ||
                EF.Functions.Like(c.Excerpt, $"%{search}%") ||
                EF.Functions.Like(c.Slug, $"%{search}%") ||
                EF.Functions.Like(c.CanonicalPath, $"%{search}%") ||
                EF.Functions.Like(c.Locale, $"%{search}%"));
        }

        return query.OrderByDescending(c => c.PublishedAt ?? c.UpdatedAt ?? c.CreatedAt);
    }
}
