using MediatR;
using Microsoft.EntityFrameworkCore;
using Vitarish.Identity.Core.Parties.Application.Queries;
using Vitarish.Identity.Core.Parties.Domain.Entities;
using Vitarish.Identity.Infrastructure.Parties.Data;

namespace Vitarish.Identity.Infrastructure.Parties.Handlers;

internal sealed class GetTenantsByClientIdHandler(PartiesDbContext db) : IRequestHandler<GetTenantsByClientId, IEnumerable<Tenant>>
{
    public async Task<IEnumerable<Tenant>> Handle(GetTenantsByClientId request, CancellationToken cancellationToken)
    {
        return await db.Tenants
            .AsNoTracking()
            .Where(t => t.ClientId == request.ClientId)
            .ToListAsync(cancellationToken);
    }
}
