using MediatR;
using Microsoft.AspNetCore.Identity;
using Vitarish.Users.Application.Queries;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Users.Infrastructure.Handlers;

internal class GetUsersByRoleQueryHandler(UserManager<User> userManager) : IRequestHandler<GetUsersByRoleQuery, IEnumerable<User>>
{
    public async Task<IEnumerable<User>> Handle(GetUsersByRoleQuery request, CancellationToken cancellationToken)
    {
        IList<User> users = await userManager.GetUsersInRoleAsync(request.RoleName);
        return users;
    }
}
