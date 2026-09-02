using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Distributed;
using Vitarish.Users.Application.Commands;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Extensions;
using Vitarish.Authentication.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Vitarish.Users.Contracts.Responses;
using Vitarish.Users.Infrastructure.Data;

namespace Vitarish.Users.Infrastructure.Handlers;

public class UpdateUserCommandHandler(UsersDbContext dbContext, UserManager<User> userManager, IDistributedCache cache, IAuthenticationActiveSessionQuery activeSessionQuery) : IRequestHandler<UpdateUserCommand, IdentityResult>
{
    public async Task<IdentityResult> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        User? current = await dbContext.Users.AsNoTracking().SingleOrDefaultAsync(user => user.Id == request.User.Id, cancellationToken);
        bool usernameChanged = current is not null &&
            (!string.Equals(current.UserName, request.User.UserName, StringComparison.Ordinal) ||
             !string.Equals(current.NormalizedUserName, request.User.NormalizedUserName, StringComparison.Ordinal));
        if (usernameChanged && await activeSessionQuery.HasActiveSessionAsync(request.User.Id, cancellationToken))
        {
            return IdentityResult.Failed(new IdentityError
            {
                Code = UsersCommandErrorCodes.ActiveSessionConflict,
                Description = UsersCommandErrorCodes.ActiveSessionConflictDescription
            });
        }
        IdentityResult result = await userManager.UpdateAsync(request.User);
        if (result.Succeeded)
        {
            await UserCacheExtensions.SetUserAsync(cache, request.User, cancellationToken);
            await UserCacheExtensions.RemoveUsersAsync(cache, cancellationToken);
        }

        return result;
    }
}
