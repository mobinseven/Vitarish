using MediatR;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Vitarish.Users.Application.Queries;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Domain.Models;

namespace Vitarish.Users.Infrastructure.Handlers;

public class GetUserClaimsQueryHandler(UserManager<User> manager) : IRequestHandler<GetUserClaimsQuery, IEnumerable<ClaimDto>>
{
    public async Task<IEnumerable<ClaimDto>> Handle(GetUserClaimsQuery request, CancellationToken cancellationToken)
    {
        User user = await manager.FindByIdAsync(request.UserId) ?? throw new InvalidDataException("user Id is invalid");
        IList<Claim> claims = await manager.GetClaimsAsync(user);
        return claims.Select(c => new ClaimDto(c.Type, c.Value)).ToList();
    }
}
