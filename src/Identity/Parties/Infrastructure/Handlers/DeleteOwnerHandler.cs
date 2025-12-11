using MediatR;
using Microsoft.EntityFrameworkCore;
using Vitarish.Parties.Application.Commands;
using Vitarish.Parties.Domain.Entities;
using Vitarish.Parties.Infrastructure.Data;

namespace Vitarish.Parties.Infrastructure.Handlers;

public sealed class DeleteOwnerHandler(PartiesDbContext db) : IRequestHandler<DeleteOwner>
{
    public async Task Handle(DeleteOwner request, CancellationToken cancellationToken)
    {
        Owner? owner = await db.Owners.FirstOrDefaultAsync(o => o.Id == request.Id, cancellationToken);
        if (owner is null)
        {
            return;
        }

        _ = db.Owners.Remove(owner);
        _ = await db.SaveChangesAsync(cancellationToken);
    }
}
