using Injectio.Attributes;
using Microsoft.EntityFrameworkCore;
using Vitarish.Entities.Server.Repositories;
using Vitarish.Parties.Domain.Descriptors;
using Vitarish.Parties.Domain.Entities;
using Vitarish.Parties.Infrastructure.Data;

namespace Vitarish.Parties.Infrastructure.Repositories;

[RegisterTransient]
internal class OwnerRepository(PartiesDbContext dbContext)
    : EntityRepository<Owner, OwnerDescriptor, PartiesDbContext>(dbContext),
      IEntityRepository<Owner, OwnerDescriptor>
{
    protected override IQueryable<Owner> Apply(IQueryable<Owner> query, OwnerDescriptor descriptor)
    {
        query = base.Apply(query, descriptor);

        if (!string.IsNullOrWhiteSpace(descriptor.Name))
        {
            query = query.Where(o => o.Name.Contains(descriptor.Name));
        }

        return query;
    }

    protected override bool IsDuplicateKeyException(DbUpdateException exception) => false;
}
