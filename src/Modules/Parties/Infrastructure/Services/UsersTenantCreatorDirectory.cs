using MediatR;
using Microsoft.AspNetCore.Identity;
using Vitarish.Parties.Application.Onboarding;
using Vitarish.Users.Application.Authorization;
using Vitarish.Users.Application.Queries;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Parties.Infrastructure.Services;

public sealed class UsersTenantCreatorDirectory(ISender sender) : ITenantCreatorDirectory
{
    public async Task<TenantCreator?> FindAsync(string userId, CancellationToken cancellationToken = default)
    {
        User? user = await sender.Send(new GetUserQuery(userId), cancellationToken);
        if (user is null)
            return null;

        IEnumerable<IdentityRole> roles = await sender.Send(new GetUserRolesQuery(user.Id), cancellationToken);
        return new TenantCreator(
            user.Id,
            user.PhoneNumberConfirmed || user.EmailConfirmed,
            RoleScopeSeparationPolicy.CanGrantTenantRole(roles.Select(role => role.Name ?? string.Empty)));
    }
}
