using MediatR;
using Microsoft.EntityFrameworkCore;
using Vitarish.Users.Application.Queries;
using Vitarish.Users.Domain.Models;
using Vitarish.Users.Infrastructure.Data;

namespace Vitarish.Users.Infrastructure.Handlers;

/// <summary>
/// Handler for retrieving access claim based on full request context.
/// Centralizes access decision logic using the request pattern.
/// </summary>
public class GetRequestAccessClaimQueryHandler(UsersDbContext db) : IRequestHandler<GetRequestAccessClaimQuery, ClaimDto?>
{
    public async Task<ClaimDto?> Handle(
        GetRequestAccessClaimQuery request,
        CancellationToken cancellationToken)
    {
        string userId = request.Request.UserId ?? throw new InvalidOperationException("UserId is required for access claim retrieval");
        string clientId = request.Request.ClientId;
        string prefix = clientId + ":";

        var claim = await db.UserClaims
            .AsNoTracking()
            .Where(c => c.UserId == userId &&
                c.ClaimType == IdentityAccessConstants.AccessClaim &&
                EF.Functions.Like(c.ClaimValue, prefix + "%"))
            .Select(c => new { c.ClaimType, c.ClaimValue })
            .SingleOrDefaultAsync(cancellationToken);

        return claim is null ? null : new ClaimDto(claim.ClaimType, claim.ClaimValue);
    }
}
