using Vitarish.Sea.Infrastructure.Data;

namespace Vitarish.Sea.Infrastructure.Repositories;

[RegisterTransient]
internal class EventRoleRepository(SeaDbContext dbContext)
    : EntityRepository<EventRole, EventRoleDescriptor, SeaDbContext>(dbContext),
      IEntityRepository<EventRole, EventRoleDescriptor>
{
    protected override IQueryable<EventRole> Apply(IQueryable<EventRole> query, EventRoleDescriptor descriptor)
    {
        query = base.Apply(query, descriptor);

        if(descriptor.EventId is not null)
        {
            query = query.Where(er => er.EventId == descriptor.EventId);
        }

        if(descriptor.RoleId is not null)
        {
            query = query.Where(er => er.RoleId == descriptor.RoleId);
        }

        if(descriptor.Type is not null)
        {
            query = query.Where(er => er.Type == descriptor.Type);
        }

        return query;
    }

    protected override bool IsDuplicateKeyException(DbUpdateException exception)
    {
        return false;
    }
}
