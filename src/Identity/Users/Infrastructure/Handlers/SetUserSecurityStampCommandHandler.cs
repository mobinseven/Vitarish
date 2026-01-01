using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Distributed;
using Vitarish.Users.Application.Commands;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Extensions;

namespace Vitarish.Users.Infrastructure.Handlers;

internal sealed class SetUserSecurityStampCommandHandler(
    UserManager<User> userManager,
    IDistributedCache cache) : IRequestHandler<SetUserSecurityStampCommand, IdentityResult>
{
    public async Task<IdentityResult> Handle(SetUserSecurityStampCommand request, CancellationToken cancellationToken)
    {
        request.User.SecurityStamp = request.SecurityStamp;
        IdentityResult result = await userManager.UpdateAsync(request.User);
        if (result.Succeeded)
        {
            await UserCacheExtensions.SetUserAsync(cache, request.User, cancellationToken);
            await UserCacheExtensions.RemoveUsersAsync(cache, cancellationToken);
        }

        return result;
    }
}
