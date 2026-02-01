using Injectio.Attributes;
using Microsoft.EntityFrameworkCore;
using Vitarish.Entities.Server.Repositories;
using Vitarish.Parties.Application.Descriptors;
using Vitarish.Parties.Domain.Entities;
using Vitarish.Parties.Infrastructure.Data;

namespace Vitarish.Parties.Infrastructure.Repositories;

[RegisterTransient]
internal class TenantIndicatorRepository(PartiesDbContext dbContext)
    : EntityRepository<TenantIndicator, TenantIndicatorDescriptor, PartiesDbContext>(dbContext),
      IEntityRepository<TenantIndicator, TenantIndicatorDescriptor>
{
    protected override IQueryable<TenantIndicator> Apply(IQueryable<TenantIndicator> query, TenantIndicatorDescriptor descriptor)
    {
        query = base.Apply(query, descriptor);

        if (descriptor.TenantId is not null)
        {
            query = query.Where(ti => ti.TenantId == descriptor.TenantId);
        }

        if (descriptor.Method is not null)
        {
            query = query.Where(ti => ti.Method == descriptor.Method);
        }

        if (!string.IsNullOrWhiteSpace(descriptor.Value))
        {
            query = query.Where(ti => ti.Value == descriptor.Value);
        }

        if (descriptor.Status is not null)
        {
            query = query.Where(ti => ti.Status == descriptor.Status);
        }

        if (descriptor.IsPrimary is not null)
        {
            query = query.Where(ti => ti.IsPrimary == descriptor.IsPrimary);
        }

        return query;
    }

    protected override bool IsDuplicateKeyException(DbUpdateException exception)
    {
        return exception.InnerException?.Message?.Contains("IX_TenantIndicators_Method_Value", StringComparison.OrdinalIgnoreCase) == true
            || exception.InnerException?.Message?.Contains("duplicate key", StringComparison.OrdinalIgnoreCase) == true
            || exception.InnerException?.Message?.Contains("unique constraint", StringComparison.OrdinalIgnoreCase) == true;
    }
}
