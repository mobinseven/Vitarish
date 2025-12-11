using MediatR;
using Microsoft.EntityFrameworkCore;
using Vitarish.Parties.Application.Queries;
using Vitarish.Parties.Domain.Entities;
using Vitarish.Parties.Infrastructure.Data;

namespace Vitarish.Parties.Infrastructure.Handlers;

public sealed class GetOwnerHandler(PartiesDbContext db) : IRequestHandler<GetOwner, Owner?>
{
    public async Task<Owner?> Handle(GetOwner request, CancellationToken cancellationToken) =>
        await db.Owners.AsNoTracking().FirstOrDefaultAsync(o => o.Id == request.Id, cancellationToken);
}
