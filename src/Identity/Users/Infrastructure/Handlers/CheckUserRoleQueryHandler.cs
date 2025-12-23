using MediatR;
using Microsoft.AspNetCore.Identity;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Users.Application.Queries;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Users.Infrastructure.Handlers;

internal class CheckUserRoleQueryHandler(UserManager<User> userManager, IRequestContext requestContext) : IRequestHandler<CheckUserRoleQuery, bool>
{
    public async Task<bool> Handle(CheckUserRoleQuery request, CancellationToken cancellationToken)
    {
        User user = await userManager.FindByIdAsync(requestContext.UserId ?? throw new InvalidDataException("IRequestContext provided a null user ID")) ?? throw new InvalidDataException("IRequestContext provided a non-existant user ID.");

        string needed = $"{requestContext.TenantId}:{request.RoleName}";
        return await userManager.IsInRoleAsync(user, needed);
    }
}
