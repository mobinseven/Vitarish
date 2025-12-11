using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Vitarish.Users.Application.Commands;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Extensions;

namespace Vitarish.Users.Infrastructure.Handlers;

public class CreateUserWithEmailCommandHandler(UserManager<User> userManager, IDistributedCache cache) : IRequestHandler<CreateUserWithEmailCommand, User?>
{
    public async Task<User?> Handle(CreateUserWithEmailCommand request, CancellationToken cancellationToken)
    {
        User? user = await userManager.Users.SingleOrDefaultAsync(u => u.Email == request.Email, cancellationToken);
        user ??= await userManager.CreateUserWithoutPasswordAsync(
            request.Email,
            request.FirstName,
            request.LastName,
            u => userManager.SetEmailAsync(u, request.Email));

        await UserCacheExtensions.SetUserAsync(cache, user, cancellationToken);
        await UserCacheExtensions.RemoveUsersAsync(cache, cancellationToken);
        return user;
    }
}
