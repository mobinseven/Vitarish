using MediatR;
using Microsoft.EntityFrameworkCore;
using Vitarish.Users.Application.Queries;
using Vitarish.Users.Domain.Models;
using Vitarish.Users.Infrastructure.Data;

namespace Vitarish.Users.Infrastructure.Handlers;

public class GetUserClaimsQueryHandler(UsersDbContext dbContext) : IRequestHandler<GetUserClaimsQuery, IEnumerable<ClaimDto>>
{
    public async Task<IEnumerable<ClaimDto>> Handle(GetUserClaimsQuery request, CancellationToken cancellationToken)
    {
        bool userExists = await dbContext.Users.AsNoTracking()
            .AnyAsync(user => user.Id == request.UserId, cancellationToken);
        return !userExists
            ? throw new InvalidDataException("user Id is invalid")
            : (IEnumerable<ClaimDto>)await dbContext.UserClaims.AsNoTracking()
            .Where(claim => claim.UserId == request.UserId)
            .Select(claim => new ClaimDto(claim.ClaimType, claim.ClaimValue))
            .ToListAsync(cancellationToken);
    }
}
