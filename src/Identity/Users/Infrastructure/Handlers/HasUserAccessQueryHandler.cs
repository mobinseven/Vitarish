using MediatR;
using Microsoft.EntityFrameworkCore;
using Vitarish.Users.Application.Queries;
using Vitarish.Users.Domain.Models;
using Vitarish.Users.Infrastructure.Data;

namespace Vitarish.Users.Infrastructure.Handlers;

public class HasUserAccessQueryHandler(UsersDbContext db) : IRequestHandler<HasUserAccessQuery, bool>
{
    public async Task<bool> Handle(
        HasUserAccessQuery request,
        CancellationToken cancellationToken)
    {
        string prefix = request.ClientId + ":";          // compile‑time concatenation
        return await db.UserClaims
            .AsNoTracking()                           // read‑only, faster
            .Where(c =>
                c.UserId == request.UserId &&
                c.ClaimType == IdentityAccessConstants.AccessClaim &&
                EF.Functions.Like(c.ClaimValue, prefix + "%")) // LIKE 'cid:%'
            .AnyAsync(cancellationToken);              // 0/1 row, no materialisation
    }
}
