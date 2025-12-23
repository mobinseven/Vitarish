using MediatR;
using Microsoft.AspNetCore.Identity;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Users.Application.Queries;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Users.Infrastructure.Handlers;

internal class GetUsersByRoleQueryHandler(UserManager<User> userManager, IRequestContext requestContext) : IRequestHandler<GetUsersByRoleQuery, IEnumerable<User>>
{
    public async Task<IEnumerable<User>> Handle(GetUsersByRoleQuery request, CancellationToken cancellationToken)
    {
        string needed = $"{requestContext.TenantId}:{request.RoleName}";
        IList<User> users = await userManager.GetUsersInRoleAsync(needed);
        return users;
    }
}
