using System.Linq.Expressions;

namespace Vitarish.Sea.Infrastructure.Repositories;

[RegisterTransient]
internal class AttachmentRepository(SeaDbContext dbContext)
    : EntityRepository<Attachment, AttachmentDescriptor, SeaDbContext>(dbContext),
      IEntityRepository<Attachment, AttachmentDescriptor>
{
    protected override IQueryable<Attachment> Apply(IQueryable<Attachment> query, AttachmentDescriptor descriptor)
    {
        query = base.Apply(query, descriptor);

        if (descriptor.EventId is not null)
        {
            query = query.Where(a => a.EventId == descriptor.EventId);
        }

        if (!string.IsNullOrWhiteSpace(descriptor.Search))
        {
            IReadOnlyCollection<string> patterns = EntitySearchHelper.CreateSearchPatterns(descriptor.Search);

            if (patterns.Count > 0)
            {
                Expression<Func<Attachment, bool>> predicate = EntitySearchHelper.BuildSearchPredicate<Attachment>(
                    patterns,
                    a => a.Content,
                    a => a.ContentType,
                    a => a.ContentThumbnail);

                query = query.Where(predicate);
            }
        }

        if (!string.IsNullOrWhiteSpace(descriptor.ContentType))
        {
            query = query.Where(a => a.ContentType == descriptor.ContentType);
        }

        if (!string.IsNullOrWhiteSpace(descriptor.ContentThumbnail))
        {
            query = query.Where(a => a.ContentThumbnail == descriptor.ContentThumbnail);
        }

        return query;
    }

    protected override bool IsDuplicateKeyException(DbUpdateException exception) => false;
}
