using MediatR;
using Microsoft.EntityFrameworkCore;
using Vitarish.Identity.Core.Parties.Application.Queries;
using Vitarish.Identity.Core.Parties.Domain.Entities;
using Vitarish.Identity.Infrastructure.Parties.Data;

namespace Vitarish.Identity.Infrastructure.Parties.Handlers;

internal sealed class GetOwnerHandler(PartiesDbContext db) : IRequestHandler<GetOwner, Owner?>
{
    public async Task<Owner?> Handle(GetOwner request, CancellationToken cancellationToken)
    {
        return await db.Owners.AsNoTracking().FirstOrDefaultAsync(o => o.Id == request.Id, cancellationToken);
    }
}
