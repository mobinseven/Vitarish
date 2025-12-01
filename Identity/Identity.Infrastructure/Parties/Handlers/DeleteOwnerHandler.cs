using MediatR;
using Microsoft.EntityFrameworkCore;
using Vitarish.Base.Domain.Results;
using Vitarish.Identity.Core.Parties.Application.Commands;
using Vitarish.Identity.Core.Parties.Domain.Entities;
using Vitarish.Identity.Infrastructure.Parties.Data;

namespace Vitarish.Identity.Infrastructure.Parties.Handlers;

internal sealed class DeleteOwnerHandler(PartiesDbContext db) : IRequestHandler<DeleteOwner, Result>
{
    public async Task<Result> Handle(DeleteOwner request, CancellationToken cancellationToken)
    {
        Owner? owner = await db.Owners.FirstOrDefaultAsync(o => o.Id == request.Id, cancellationToken);
        if (owner is null)
        {
            return Result.Failure(Error.NotFound("Owner.NotFound", $"Owner with ID '{request.Id}' was not found."));
        }

        _ = db.Owners.Remove(owner);
        _ = await db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
