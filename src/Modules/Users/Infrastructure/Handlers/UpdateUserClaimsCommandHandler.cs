using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Security.Claims;
using Vitarish.Users.Application.Commands;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Data;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Users.Contracts.Responses;

namespace Vitarish.Users.Infrastructure.Handlers;

public class UpdateUserClaimsCommandHandler(UsersDbContext dbContext, UserManager<User> userManager, IAuthenticationActiveSessionQuery activeSessionQuery) : IRequestHandler<UpdateUserClaimsCommand, IdentityResult>
{
    private class ClaimEqualityComparer : IEqualityComparer<Claim>
    {
        public bool Equals(Claim? c1, Claim? c2) => (c1 == null && c2 == null) || (c1 != null && c2 != null && c1.Type == c2.Type && c1.Value == c2.Value);
        public int GetHashCode(Claim claim) => (claim.Type + claim.Value).GetHashCode();
    }

    public async Task<IdentityResult> Handle(UpdateUserClaimsCommand request, CancellationToken cancellationToken)
    {
        IList<Claim> oldClaims = await userManager.GetClaimsAsync(request.User);
        List<Claim> claimsToRemove = [.. oldClaims.Except(request.Claims, new ClaimEqualityComparer())];
        List<Claim> claimsToAdd = [.. request.Claims.Except(oldClaims, new ClaimEqualityComparer())];
        if (claimsToRemove.Count == 0 && claimsToAdd.Count == 0)
            return IdentityResult.Success;

        if (await activeSessionQuery.HasActiveSessionAsync(request.User.Id, cancellationToken))
        {
            return IdentityResult.Failed(new IdentityError
            {
                Code = UsersCommandErrorCodes.ActiveSessionConflict,
                Description = UsersCommandErrorCodes.ActiveSessionConflictDescription
            });
        }

        IExecutionStrategy strategy = dbContext.Database.CreateExecutionStrategy();
        IdentityResult? outcome = null;
        await strategy.ExecuteAsync(async ct =>
        {
            await using IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync(ct);
            IdentityResult result = await userManager.RemoveClaimsAsync(request.User, claimsToRemove);
            if (result.Succeeded)
                result = await userManager.AddClaimsAsync(request.User, claimsToAdd);
            if (!result.Succeeded)
            {
                await transaction.RollbackAsync(CancellationToken.None);
                outcome = result;
                return;
            }
            await transaction.CommitAsync(ct);
            outcome = result;
        }, cancellationToken);
        return outcome!;
    }
}
