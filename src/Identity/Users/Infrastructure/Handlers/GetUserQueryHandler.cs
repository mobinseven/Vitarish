using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Distributed;
using Vitarish.Users.Application.Queries;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Extensions;

namespace Vitarish.Users.Infrastructure.Handlers;

public class GetUserQueryHandler(UserManager<User> userManager, IDistributedCache cache) : IRequestHandler<GetUserQuery, User?>
{
    public async Task<User?> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        if (await UserCacheExtensions.GetUserAsync(cache, request.Id, cancellationToken) is { } cached)
        {
            return cached;
        }

        User? user = await userManager.FindByIdAsync(request.Id);
        if (user is not null)
        {
            await UserCacheExtensions.SetUserAsync(cache, user, cancellationToken);
        }

        return user;
    }
}
