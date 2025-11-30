
using MediatR;
using Microsoft.EntityFrameworkCore;
using Vorn.Aaas.Parties.Domain.Entities;
using Vorn.Aaas.Parties.Infrastructure.Data;

namespace Vorn.Aaas.Parties.Application.Queries;
/// <summary>
/// Query to retrieve all <see cref="Tenant"/> entities.
/// </summary>
public sealed record GetTenants : IRequest<IEnumerable<Tenant>>;

internal sealed class GetTenantsHandler(PartiesDbContext db) : IRequestHandler<GetTenants, IEnumerable<Tenant>>
{
    public async Task<IEnumerable<Tenant>> Handle(GetTenants request, CancellationToken cancellationToken) =>
        await db.Tenants.AsNoTracking().ToListAsync(cancellationToken);
}
