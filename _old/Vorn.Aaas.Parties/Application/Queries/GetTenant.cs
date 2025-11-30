using MediatR;
using Microsoft.EntityFrameworkCore;
using Vorn.Aaas.Parties.Domain.Entities;
using Vorn.Aaas.Parties.Infrastructure.Data;

namespace Vorn.Aaas.Parties.Application.Queries;
/// <summary>
/// Query for a single <see cref="Tenant"/> by identifier.
/// </summary>
public sealed record GetTenant(Guid Id) : IRequest<Tenant?>;

internal sealed class GetTenantHandler(PartiesDbContext db) : IRequestHandler<GetTenant, Tenant?>
{
    public async Task<Tenant?> Handle(GetTenant request, CancellationToken cancellationToken) =>
        await db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);
}
