using MediatR;
using Microsoft.EntityFrameworkCore;
using Vitarish.Base.Domain.Results;
using Vitarish.Identity.Core.Parties.Application.Commands;
using Vitarish.Identity.Infrastructure.Parties.Data;

namespace Vitarish.Identity.Infrastructure.Parties.Handlers;

internal sealed class UpdateOwnerHandler(PartiesDbContext db) : IRequestHandler<UpdateOwner, Result>
{
    public async Task<Result> Handle(UpdateOwner request, CancellationToken cancellationToken)
    {
        var owner = await db.Owners.FirstOrDefaultAsync(o => o.Id == request.Id, cancellationToken);
        if (owner is null)
        {
            return Result.Failure(Error.NotFound("Owner.NotFound", $"Owner with ID '{request.Id}' was not found."));
        }

        owner.Update(request.Name, request.LogoUrl, request.Description, request.ModifiedBy);
        _ = await db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
