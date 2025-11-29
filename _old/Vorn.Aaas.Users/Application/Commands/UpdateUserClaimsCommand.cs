using MediatR;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using FluentValidation;
using Vorn.Aaas.Users.Domain.Entities;

namespace Vorn.Aaas.Users.Application.Commands;

public record UpdateUserClaimsCommand(User User, IEnumerable<Claim> Claims) : IRequest<IdentityResult>;

public class UpdateUserClaimsCommandValidator : AbstractValidator<UpdateUserClaimsCommand>
{
    public UpdateUserClaimsCommandValidator()
    {
        RuleFor(c => c.User).NotNull();
        RuleFor(c => c.Claims).NotNull();
    }
}

internal class UpdateUserClaimsCommandHandler(UserManager<User> userManager) : IRequestHandler<UpdateUserClaimsCommand, IdentityResult>
{
    private class ClaimEqualityComparer : IEqualityComparer<Claim>
    {
        public bool Equals(Claim? c1, Claim? c2)
        {
            if (c1 == null && c2 == null) return true;
            if (c1 == null || c2 == null) return false;
            return c1.Type == c2.Type && c1.Value == c2.Value;
        }
        public int GetHashCode(Claim claim) => (claim.Type + claim.Value).GetHashCode();
    }

    public async Task<IdentityResult> Handle(UpdateUserClaimsCommand request, CancellationToken cancellationToken)
    {
        IList<Claim> oldClaims = await userManager.GetClaimsAsync(request.User);
        List<Claim> claimsToRemove = oldClaims.Except(request.Claims, new ClaimEqualityComparer()).ToList();
        IdentityResult res = await userManager.RemoveClaimsAsync(request.User, claimsToRemove);
        if (!res.Succeeded)
            return res;
        List<Claim> claimsToAdd = request.Claims.Except(oldClaims, new ClaimEqualityComparer()).ToList();
        res = await userManager.AddClaimsAsync(request.User, claimsToAdd);
        return res;
    }
}
