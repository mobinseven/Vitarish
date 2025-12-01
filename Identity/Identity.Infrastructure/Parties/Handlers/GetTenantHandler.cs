using MediatR;
using Microsoft.EntityFrameworkCore;
using Vitarish.Identity.Core.Parties.Application.Queries;
using Vitarish.Identity.Core.Parties.Domain.Entities;
using Vitarish.Identity.Infrastructure.Parties.Data;

namespace Vitarish.Identity.Infrastructure.Parties.Handlers;

internal sealed class GetTenantHandler(PartiesDbContext db) : IRequestHandler<GetTenant, Tenant?>
{
    public async Task<Tenant?> Handle(GetTenant request, CancellationToken cancellationToken)
    {
        return await db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);
    }
}
