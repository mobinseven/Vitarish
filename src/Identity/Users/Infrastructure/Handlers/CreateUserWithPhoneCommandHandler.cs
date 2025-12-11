using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Vitarish.Users.Application.Commands;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Extensions;

namespace Vitarish.Users.Infrastructure.Handlers;

public class CreateUserWithPhoneCommandHandler(UserManager<User> userManager, IDistributedCache cache) : IRequestHandler<CreateUserWithPhoneCommand, User?>
{
    public async Task<User?> Handle(CreateUserWithPhoneCommand request, CancellationToken cancellationToken)
    {
        User? user = await userManager.Users.SingleOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumber, cancellationToken);
        user ??= await userManager.CreateUserWithoutPasswordAsync(
            request.PhoneNumber,
            request.FirstName,
            request.LastName,
            u => userManager.SetPhoneNumberAsync(u, request.PhoneNumber));

        await UserCacheExtensions.SetUserAsync(cache, user, cancellationToken);
        await UserCacheExtensions.RemoveUsersAsync(cache, cancellationToken);
        return user;
    }
}
