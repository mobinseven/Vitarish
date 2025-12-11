using MediatR;
using Microsoft.EntityFrameworkCore;
using Vitarish.Parties.Application.Queries;
using Vitarish.Parties.Domain.Entities;
using Vitarish.Parties.Infrastructure.Data;

namespace Vitarish.Parties.Infrastructure.Handlers;

public sealed class GetOwnersHandler(PartiesDbContext db) : IRequestHandler<GetOwners, IEnumerable<Owner>>
{
    public async Task<IEnumerable<Owner>> Handle(GetOwners request, CancellationToken cancellationToken) =>
        await db.Owners.AsNoTracking().ToListAsync(cancellationToken);
}
