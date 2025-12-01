using MediatR;
using Microsoft.EntityFrameworkCore;
using Vitarish.Identity.Core.Parties.Application.Queries;
using Vitarish.Identity.Core.Parties.Domain.Entities;
using Vitarish.Identity.Infrastructure.Parties.Data;

namespace Vitarish.Identity.Infrastructure.Parties.Handlers;

internal sealed class GetOwnersHandler(PartiesDbContext db) : IRequestHandler<GetOwners, IEnumerable<Owner>>
{
    public async Task<IEnumerable<Owner>> Handle(GetOwners request, CancellationToken cancellationToken)
    {
        return await db.Owners.AsNoTracking().ToListAsync(cancellationToken);
    }
}
