using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Distributed;
using Vitarish.Users.Application.Commands;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Extensions;

namespace Vitarish.Users.Infrastructure.Handlers;

public class SetUserPhoneNumberCommandHandler(UserManager<User> userManager, IDistributedCache cache) : IRequestHandler<SetUserPhoneNumberCommand, IdentityResult>
{
    public async Task<IdentityResult> Handle(SetUserPhoneNumberCommand request, CancellationToken cancellationToken)
    {
        User? user = await userManager.FindByIdAsync(request.Id);
        if (user is null)
        {
            return IdentityResult.Failed(new IdentityError { Description = "User not found" });
        }

        IdentityResult result = await userManager.SetPhoneNumberAsync(user, request.PhoneNumber);
        if (result.Succeeded)
        {
            await UserCacheExtensions.SetUserAsync(cache, user, cancellationToken);
            await UserCacheExtensions.RemoveUsersAsync(cache, cancellationToken);
        }

        return result;
    }
}
