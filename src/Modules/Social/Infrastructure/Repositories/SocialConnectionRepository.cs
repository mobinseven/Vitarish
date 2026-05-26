using Vitarish.Entities.Server.Data;
namespace Vitarish.Social.Infrastructure.Repositories;

[RegisterTransient]
internal sealed class SocialConnectionRepository(SocialDbContext dbContext)
    : EntityRepository<SocialConnection, SocialConnectionDescriptor, SocialDbContext>(dbContext),
      IEntityRepository<SocialConnection, SocialConnectionDescriptor>
{
    protected override IQueryable<SocialConnection> Apply(IQueryable<SocialConnection> query, SocialConnectionDescriptor descriptor)
    {
        query = base.Apply(query, descriptor);

        if (descriptor.Provider is { } provider)
        {
            query = query.Where(c => c.Provider == provider);
        }

        if (descriptor.Status is { } status)
        {
            query = query.Where(c => c.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(descriptor.ExternalAccountId))
        {
            string externalAccountId = descriptor.ExternalAccountId.Trim();
            query = query.Where(c => c.ExternalAccountId == externalAccountId);
        }

        return query;
    }

    protected override bool IsDuplicateKeyException(DbUpdateException exception)
        => DatabaseExceptions.IsUniqueConstraintViolation(exception);
}
