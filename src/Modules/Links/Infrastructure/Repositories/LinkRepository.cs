namespace Vitarish.Links.Infrastructure.Repositories;

[RegisterTransient]
internal sealed class LinkRepository(LinksDbContext dbContext)
    : EntityRepository<Link, LinkDescriptor, LinksDbContext>(dbContext),
      IEntityRepository<Link, LinkDescriptor>
{
    protected override bool IsDuplicateKeyException(DbUpdateException ex)
        => LinksDatabaseExceptions.IsUniqueConstraintViolation(ex);

    protected override IQueryable<Link> Apply(IQueryable<Link> query, LinkDescriptor descriptor)
    {
        query = base.Apply(query, descriptor);

        if (descriptor.TenantId is { } tenantId)
        {
            query = query.Where(l => l.TenantId == tenantId);
        }

        if (!string.IsNullOrWhiteSpace(descriptor.Domain))
        {
            string domain = LinkRules.NormalizeDomain(descriptor.Domain);
            query = query.Where(l => l.Domain == domain);
        }

        if (!string.IsNullOrWhiteSpace(descriptor.Slug))
        {
            string slug = descriptor.Slug.Trim();
            query = query.Where(l => l.Slug == slug);
        }

        if (descriptor.Status is { } status)
        {
            query = query.Where(l => l.Status == status);
        }

        return query;
    }
}
