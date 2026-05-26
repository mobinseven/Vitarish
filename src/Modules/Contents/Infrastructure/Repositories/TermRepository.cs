using Vitarish.Entities.Server.Data;
namespace Vitarish.Contents.Infrastructure.Repositories;

[RegisterTransient]
internal sealed class TermRepository(ContentsDbContext dbContext)
    : EntityRepository<Term, TermDescriptor, ContentsDbContext>(dbContext),
      IEntityRepository<Term, TermDescriptor>
{
    protected override bool IsDuplicateKeyException(DbUpdateException ex)
        => DatabaseExceptions.IsUniqueConstraintViolation(ex);

    protected override IQueryable<Term> Apply(IQueryable<Term> query, TermDescriptor descriptor)
    {
        query = base.Apply(query, descriptor);

        if (descriptor.Kind is { } kind)
        {
            query = query.Where(t => t.Kind == kind);
        }

        if (descriptor.ParentId is { } parentId)
        {
            query = query.Where(t => t.ParentId == parentId);
        }

        if (!string.IsNullOrWhiteSpace(descriptor.Slug))
        {
            string slug = descriptor.Slug.Trim();
            query = query.Where(t => t.Slug == slug);
        }

        if (!string.IsNullOrWhiteSpace(descriptor.Name))
        {
            string name = descriptor.Name.Trim();
            query = query.Where(t => t.Name == name);
        }

        if (!string.IsNullOrWhiteSpace(descriptor.Search))
        {
            string search = descriptor.Search.Trim();
            query = query.Where(t =>
                EF.Functions.Like(t.Slug, $"%{search}%") ||
                EF.Functions.Like(t.Name, $"%{search}%"));
        }

        return query;
    }
}
