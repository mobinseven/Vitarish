using MediatR;
using Microsoft.EntityFrameworkCore;
using Vitarish.Users.Application.Queries;
using Vitarish.Users.Domain.Models;
using Vitarish.Users.Infrastructure.Data;

namespace Vitarish.Users.Infrastructure.Handlers;

public class GetClientUserQueryHandler(UsersDbContext db) : IRequestHandler<GetClientUserQuery, UserDto?>
{
    public async Task<UserDto?> Handle(
        GetClientUserQuery request,
        CancellationToken cancellationToken)
    {
        // prefix the access‑claim value once; no per‑row Split()
        string prefix = request.ClientId + ":";

        return await (
            from u in db.Users.AsNoTracking()                         // read‑only
            join c in db.UserClaims.AsNoTracking()
                     on u.Id equals c.UserId
            where u.Id == request.UserId
                  && c.ClaimType == IdentityAccessConstants.AccessClaim
                  && EF.Functions.Like(c.ClaimValue, prefix + "%")    // LIKE 'cid:%'
            select new UserDto                                       // project directly to DTO
            {
                Id = u.Id,
                UserName = u.UserName,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                AccessClaim = new ClaimDto(c.ClaimType, c.ClaimValue),
                ProfilePreferences = u.ProfilePreferences
            })
            .SingleOrDefaultAsync(cancellationToken);                // 0/1 row by design
    }
}
