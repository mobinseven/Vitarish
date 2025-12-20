using Vitarish.Sea.Infrastructure.Data;

namespace Vitarish.Sea.Infrastructure.Repositories;

[RegisterScoped]
internal class UserRoleRepository(SeaDbContext dbContext)
    : EntityRepository<UserRole, UserRoleDescriptor, SeaDbContext>(dbContext),
      IEntityRepository<UserRole, UserRoleDescriptor>
{

    protected override IQueryable<UserRole> Apply(IQueryable<UserRole> query, UserRoleDescriptor descriptor)
    {
        query = base.Apply(query, descriptor);

        if(descriptor.UserId is not null)
        {
            query = query.Where(ur => ur.UserId == descriptor.UserId);
        }

        if(descriptor.RoleId is not null)
        {
            query = query.Where(ur => ur.RoleId == descriptor.RoleId);
        }

        if(descriptor.Type is not null)
        {
            query = query.Where(ur => ur.Type == descriptor.Type);
        }

        return query;
    }

    protected override bool IsDuplicateKeyException(DbUpdateException exception)
    {
        return false;
    }
}
