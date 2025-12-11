using MediatR;
using Microsoft.EntityFrameworkCore;
using Vitarish.Parties.Application.Commands;
using Vitarish.Parties.Domain.Entities;
using Vitarish.Parties.Infrastructure.Data;

namespace Vitarish.Parties.Infrastructure.Handlers;

public sealed class UpdateOwnerHandler(PartiesDbContext db) : IRequestHandler<UpdateOwner, Owner?>
{
    public async Task<Owner?> Handle(UpdateOwner request, CancellationToken cancellationToken)
    {
        Owner? owner = await db.Owners.FirstOrDefaultAsync(o => o.Id == request.Id, cancellationToken);
        if (owner is null)
        {
            return null;
        }

        owner.Name = request.Name;
        owner.LogoUrl = request.LogoUrl;
        owner.Description = request.Description;
        owner.ModifiedAt = DateTime.UtcNow;
        owner.ModifiedBy = request.ModifiedBy;

        _ = await db.SaveChangesAsync(cancellationToken);
        return owner;
    }
}
