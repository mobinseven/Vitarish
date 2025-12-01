using MediatR;
using Microsoft.EntityFrameworkCore;
using Vitarish.Identity.Core.Parties.Application.Queries;
using Vitarish.Identity.Core.Parties.Domain.Entities;
using Vitarish.Identity.Infrastructure.Parties.Data;

namespace Vitarish.Identity.Infrastructure.Parties.Handlers;

internal sealed class GetTenantsByOwnerHandler(PartiesDbContext db) : IRequestHandler<GetTenantsByOwner, IEnumerable<Tenant>>
{
    public async Task<IEnumerable<Tenant>> Handle(GetTenantsByOwner request, CancellationToken cancellationToken)
    {
        return await db.Tenants
            .AsNoTracking()
            .Where(t => t.OwnerId == request.OwnerId)
            .ToListAsync(cancellationToken);
    }
}
