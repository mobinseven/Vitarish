using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Distributed;
using Vitarish.Users.Application.Commands;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Extensions;

namespace Vitarish.Users.Infrastructure.Handlers;

public class UpdateUserCommandHandler(UserManager<User> userManager, IDistributedCache cache) : IRequestHandler<UpdateUserCommand, IdentityResult>
{
    public async Task<IdentityResult> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        IdentityResult result = await userManager.UpdateAsync(request.User);
        if (result.Succeeded)
        {
            await UserCacheExtensions.SetUserAsync(cache, request.User, cancellationToken);
            await UserCacheExtensions.RemoveUsersAsync(cache, cancellationToken);
        }

        return result;
    }
}
