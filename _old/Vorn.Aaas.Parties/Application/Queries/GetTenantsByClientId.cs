
using MediatR;
using Microsoft.EntityFrameworkCore;
using Vorn.Aaas.Parties.Domain.Entities;
using Vorn.Aaas.Parties.Infrastructure.Data;

namespace Vorn.Aaas.Parties.Application.Queries;
/// <summary>
/// Query to retrieve all <see cref="Tenant"/> entities for a specific client.
/// </summary>
public sealed record GetTenantsByClientId(string ClientId) : IRequest<IEnumerable<Tenant>>;

internal sealed class GetTenantsByClientIdHandler(PartiesDbContext db) : IRequestHandler<GetTenantsByClientId, IEnumerable<Tenant>>
{
    public async Task<IEnumerable<Tenant>> Handle(GetTenantsByClientId request, CancellationToken cancellationToken) =>
        await db.Tenants.AsNoTracking().Where(t => t.ClientId == request.ClientId).ToListAsync(cancellationToken);
}
