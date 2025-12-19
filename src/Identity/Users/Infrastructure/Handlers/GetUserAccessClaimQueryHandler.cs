using MediatR;
using Microsoft.EntityFrameworkCore;
using Vitarish.Users.Application.Queries;
using Vitarish.Users.Domain.Models;
using Vitarish.Users.Infrastructure.Data;

namespace Vitarish.Users.Infrastructure.Handlers;

public class GetUserAccessClaimQueryHandler(UsersDbContext db) : IRequestHandler<GetUserAccessClaimQuery, ClaimDto?>
{
    public async Task<ClaimDto?> Handle(
        GetUserAccessClaimQuery request,
        CancellationToken cancellationToken)
    {
        string prefix = request.ClientId + ":";
        var claim = await db.UserClaims
            .AsNoTracking()
            .Where(c => c.UserId == request.UserId &&
                c.ClaimType == IdentityAccessConstants.AccessClaim &&
                EF.Functions.Like(c.ClaimValue, prefix + "%"))
            .Select(c => new { c.ClaimType, c.ClaimValue })
            .SingleOrDefaultAsync(cancellationToken);

        return claim is null ? null : new ClaimDto(claim.ClaimType, claim.ClaimValue);
    }
}
