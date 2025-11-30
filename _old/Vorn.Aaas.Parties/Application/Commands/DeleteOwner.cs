using MediatR;
using Microsoft.EntityFrameworkCore;
using Vorn.Aaas.Parties.Domain.Entities;
using Vorn.Aaas.Parties.Infrastructure.Data;

namespace Vorn.Aaas.Parties.Application.Commands;
/// <summary>
/// Command to delete an <see cref="Domain.Entities.Owner"/>.
/// </summary>
public sealed record DeleteOwner(Guid Id) : IRequest;

internal sealed class DeleteOwnerHandler(PartiesDbContext db) : IRequestHandler<DeleteOwner>
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
