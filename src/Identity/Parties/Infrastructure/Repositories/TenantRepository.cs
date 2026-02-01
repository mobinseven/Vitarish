using Injectio.Attributes;
using Microsoft.EntityFrameworkCore;
using Vitarish.Entities.Server.Repositories;
using Vitarish.Parties.Domain.Descriptors;
using Vitarish.Parties.Domain.Entities;
using Vitarish.Parties.Infrastructure.Data;

namespace Vitarish.Parties.Infrastructure.Repositories;

[RegisterTransient]
internal class TenantRepository(PartiesDbContext dbContext)
    : EntityRepository<Tenant, TenantDescriptor, PartiesDbContext>(dbContext),
      IEntityRepository<Tenant, TenantDescriptor>
{
    protected override IQueryable<Tenant> Apply(IQueryable<Tenant> query, TenantDescriptor descriptor)
    {
        query = base.Apply(query, descriptor);

        if (descriptor.OwnerId is not null)
        {
            query = query.Where(t => t.OwnerId == descriptor.OwnerId);
        }

        if (!string.IsNullOrWhiteSpace(descriptor.Name))
        {
            query = query.Where(t => t.Name.Contains(descriptor.Name));
        }

        if (!string.IsNullOrWhiteSpace(descriptor.ClientId))
        {
            query = query.Where(t => t.ClientId == descriptor.ClientId);
        }

        return query;
    }

    protected override bool IsDuplicateKeyException(DbUpdateException exception) => false;
}
