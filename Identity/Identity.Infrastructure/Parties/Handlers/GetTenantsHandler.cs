using MediatR;
using Microsoft.EntityFrameworkCore;
using Vitarish.Identity.Core.Parties.Application.Queries;
using Vitarish.Identity.Core.Parties.Domain.Entities;
using Vitarish.Identity.Infrastructure.Parties.Data;

namespace Vitarish.Identity.Infrastructure.Parties.Handlers;

internal sealed class GetTenantsHandler(PartiesDbContext db) : IRequestHandler<GetTenants, IEnumerable<Tenant>>
{
    public async Task<IEnumerable<Tenant>> Handle(GetTenants request, CancellationToken cancellationToken)
    {
        return await db.Tenants.AsNoTracking().ToListAsync(cancellationToken);
    }
}
