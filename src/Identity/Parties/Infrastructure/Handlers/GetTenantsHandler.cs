using MediatR;
using Microsoft.EntityFrameworkCore;
using Vitarish.Parties.Application.Queries;
using Vitarish.Parties.Domain.Entities;
using Vitarish.Parties.Infrastructure.Data;

namespace Vitarish.Parties.Infrastructure.Handlers;

public sealed class GetTenantsHandler(PartiesDbContext db) : IRequestHandler<GetTenants, IEnumerable<Tenant>>
{
    public async Task<IEnumerable<Tenant>> Handle(GetTenants request, CancellationToken cancellationToken) =>
        await db.Tenants.AsNoTracking().ToListAsync(cancellationToken);
}
