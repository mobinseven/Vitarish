
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Vorn.Aaas.Parties.Domain.Entities;
using Vorn.Aaas.Parties.Infrastructure.Data;

namespace Vorn.Aaas.Parties.Application.Queries;
/// <summary>
/// Query to retrieve all <see cref="Tenant"/> entities for a specific owner.
/// </summary>
public sealed record GetTenantsByOwner(Guid OwnerId) : IRequest<IEnumerable<Tenant>>;

internal sealed class GetTenantsByOwnerHandler(PartiesDbContext db) : IRequestHandler<GetTenantsByOwner, IEnumerable<Tenant>>
{
    public async Task<IEnumerable<Tenant>> Handle(GetTenantsByOwner request, CancellationToken cancellationToken) =>
        await db.Tenants.AsNoTracking().Where(t => t.OwnerId == request.OwnerId).ToListAsync(cancellationToken);
}
