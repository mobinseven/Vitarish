using MediatR;
using Microsoft.EntityFrameworkCore;
using Vitarish.Parties.Application.Queries;
using Vitarish.Parties.Domain.Entities;
using Vitarish.Parties.Infrastructure.Data;

namespace Vitarish.Parties.Infrastructure.Handlers;

public sealed class GetTenantsByClientIdHandler(PartiesDbContext db) : IRequestHandler<GetTenantsByClientId, IEnumerable<Tenant>>
{
    public async Task<IEnumerable<Tenant>> Handle(GetTenantsByClientId request, CancellationToken cancellationToken) =>
        await db.Tenants.AsNoTracking().Where(t => t.ClientId == request.ClientId).ToListAsync(cancellationToken);
}
