using MediatR;
using Microsoft.AspNetCore.Identity;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Users.Application.Commands;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Users.Infrastructure.Handlers;

internal sealed class AddUserToRoleCommandHandler(
    UserManager<User> userManager,
    IRequestContext requestContext) : IRequestHandler<AddUserToRoleCommand, IdentityResult>
{
    public Task<IdentityResult> Handle(AddUserToRoleCommand request, CancellationToken cancellationToken)
    {
        string roleName = RoleForTenant(request.RoleName);
        return userManager.AddToRoleAsync(request.User, roleName);
    }

    private string RoleForTenant(string roleName) =>
        string.IsNullOrWhiteSpace(requestContext.TenantId) ? roleName : $"{requestContext.TenantId}:{roleName}";
}
