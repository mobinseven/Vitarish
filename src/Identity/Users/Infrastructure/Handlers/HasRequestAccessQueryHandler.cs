using MediatR;
using Microsoft.EntityFrameworkCore;
using Vitarish.Users.Application.Queries;
using Vitarish.Users.Domain.Models;
using Vitarish.Users.Infrastructure.Data;

namespace Vitarish.Users.Infrastructure.Handlers;

/// <summary>
/// Handler for checking access based on full request context.
/// Centralizes access decision logic using the request pattern.
/// </summary>
public class HasRequestAccessQueryHandler(UsersDbContext db) : IRequestHandler<HasRequestAccessQuery, bool>
{
    public async Task<bool> Handle(
        HasRequestAccessQuery request,
        CancellationToken cancellationToken)
    {
        string userId = request.Request.UserId ?? throw new InvalidOperationException("UserId is required for access check");
        string clientId = request.Request.ClientId;
        string prefix = clientId + ":";

        return await db.UserClaims
            .AsNoTracking()
            .Where(c =>
                c.UserId == userId &&
                c.ClaimType == IdentityAccessConstants.AccessClaim &&
                EF.Functions.Like(c.ClaimValue, prefix + "%"))
            .AnyAsync(cancellationToken);
    }
}
