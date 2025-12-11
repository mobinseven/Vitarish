using MediatR;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Vitarish.Users.Application.Commands;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Users.Infrastructure.Handlers;

public class UpdateUserClaimsCommandHandler(UserManager<User> userManager) : IRequestHandler<UpdateUserClaimsCommand, IdentityResult>
{
    private class ClaimEqualityComparer : IEqualityComparer<Claim>
    {
        public bool Equals(Claim? c1, Claim? c2) => (c1 == null && c2 == null) || (c1 != null && c2 != null && c1.Type == c2.Type && c1.Value == c2.Value);
        public int GetHashCode(Claim claim) => (claim.Type + claim.Value).GetHashCode();
    }

    public async Task<IdentityResult> Handle(UpdateUserClaimsCommand request, CancellationToken cancellationToken)
    {
        IList<Claim> oldClaims = await userManager.GetClaimsAsync(request.User);
        List<Claim> claimsToRemove = oldClaims.Except(request.Claims, new ClaimEqualityComparer()).ToList();
        IdentityResult res = await userManager.RemoveClaimsAsync(request.User, claimsToRemove);
        if (!res.Succeeded)
        {
            return res;
        }

        List<Claim> claimsToAdd = request.Claims.Except(oldClaims, new ClaimEqualityComparer()).ToList();
        res = await userManager.AddClaimsAsync(request.User, claimsToAdd);
        return res;
    }
}
