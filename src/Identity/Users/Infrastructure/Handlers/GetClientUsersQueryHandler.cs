using MediatR;
using Microsoft.EntityFrameworkCore;
using Vitarish.Users.Application.Queries;
using Vitarish.Users.Domain.Models;
using Vitarish.Users.Infrastructure.Data;

namespace Vitarish.Users.Infrastructure.Handlers;

public class GetClientUsersQueryHandler(UsersDbContext db) : IRequestHandler<GetClientUsersQuery, IEnumerable<UserDto>>
{
    public async Task<IEnumerable<UserDto>> Handle(GetClientUsersQuery request, CancellationToken cancellationToken)
    {
        List<UserDto> users = await db.UserClaims
            .AsNoTracking()
            .Where(uc => uc.ClaimType == IdentityAccessConstants.AccessClaim)
            .Where(uc => uc.ClaimValue!.StartsWith(request.ClientId))
            .Join(db.Users, uc => uc.UserId, u => u.Id, (uc, u) => new UserDto
            {
                Id = u.Id,
                UserName = u.UserName,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                AccessClaim = new ClaimDto(uc.ClaimType, uc.ClaimValue)
            })
            .Distinct()
            .ToListAsync(cancellationToken);

        return users.Where(u => u.AccessClaim!.Value!.Split(':').First() == request.ClientId).ToList();
    }
}
