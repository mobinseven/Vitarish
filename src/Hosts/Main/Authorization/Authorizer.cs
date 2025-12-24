using Duende.IdentityServer.Extensions;
using Microsoft.AspNetCore.Authorization;
using Vitarish.Users.Infrastructure;

namespace Vitarish.Main.Authorization;

public sealed class Authorizer(UserManager<User> userManager) : AuthorizationHandler<AuthorizerRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AuthorizerRequirement requirement)
    {
        if (context.User.IsInRole(requirement.Role))
        {
            context.Succeed(requirement);
            return;
        }
        if (context.User.Claims.Any(c => c.Type == IdentityAccessConstants.AccessClaim && c.Value == requirement.Role))
        {
            context.Succeed(requirement);
            return;
        }
        var user = await userManager.FindByIdAsync(context.User.GetSubjectId());
        if (user is not null && await userManager.IsInRoleAsync(user, requirement.Role))
        {
            context.Succeed(requirement);
            return;
        }
    }
}
