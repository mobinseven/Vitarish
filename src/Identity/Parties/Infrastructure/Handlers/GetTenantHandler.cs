using MediatR;
using Microsoft.EntityFrameworkCore;
using Vitarish.Parties.Application.Queries;
using Vitarish.Parties.Domain.Entities;
using Vitarish.Parties.Infrastructure.Data;

namespace Vitarish.Parties.Infrastructure.Handlers;

public sealed class GetTenantHandler(PartiesDbContext db) : IRequestHandler<GetTenant, Tenant?>
{
    public async Task<Tenant?> Handle(GetTenant request, CancellationToken cancellationToken) =>
        await db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);
}
