namespace Vitarish.Social.Infrastructure.Repositories;

[RegisterTransient]
internal sealed class SocialConnectionTokenRepository(SocialDbContext dbContext)
    : EntityRepository<SocialConnectionToken, SocialConnectionTokenDescriptor, SocialDbContext>(dbContext),
      IEntityRepository<SocialConnectionToken, SocialConnectionTokenDescriptor>
{
    protected override IQueryable<SocialConnectionToken> Apply(IQueryable<SocialConnectionToken> query, SocialConnectionTokenDescriptor descriptor)
    {
        query = base.Apply(query, descriptor);

        if (descriptor.ConnectionId is { } connectionId)
        {
            query = query.Where(t => t.ConnectionId == connectionId);
        }

        return query;
    }

    protected override bool IsDuplicateKeyException(DbUpdateException exception)
        => SocialDatabaseExceptions.IsUniqueConstraintViolation(exception);
}
