using Vitarish.Entities.Server.Data;
namespace Vitarish.Contents.Infrastructure.Repositories;

[RegisterTransient]
internal sealed class ContentRelationRepository(ContentsDbContext dbContext)
    : EntityRepository<ContentRelation, ContentRelationDescriptor, ContentsDbContext>(dbContext),
      IEntityRepository<ContentRelation, ContentRelationDescriptor>
{
    protected override bool IsDuplicateKeyException(DbUpdateException ex)
        => DatabaseExceptions.IsUniqueConstraintViolation(ex);

    protected override IQueryable<ContentRelation> Apply(IQueryable<ContentRelation> query, ContentRelationDescriptor descriptor)
    {
        query = base.Apply(query, descriptor);

        if (descriptor.FromContentId is { } fromContentId)
        {
            query = query.Where(cr => cr.FromContentId == fromContentId);
        }

        if (descriptor.ToContentId is { } toContentId)
        {
            query = query.Where(cr => cr.ToContentId == toContentId);
        }

        if (descriptor.RelationType is { } relationType)
        {
            query = query.Where(cr => cr.RelationType == relationType);
        }

        return query;
    }
}
