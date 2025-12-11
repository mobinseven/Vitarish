using MediatR;
using Microsoft.AspNetCore.Identity;
using Vitarish.Users.Application.Queries;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Users.Infrastructure.Handlers;

public class UserHasPasswordQueryHandler(UserManager<User> userManager) : IRequestHandler<UserHasPasswordQuery, bool>
{
    public async Task<bool> Handle(UserHasPasswordQuery request, CancellationToken cancellationToken)
    {
        User? user = await userManager.FindByIdAsync(request.Id);
        return user is not null && await userManager.HasPasswordAsync(user);
    }
}
