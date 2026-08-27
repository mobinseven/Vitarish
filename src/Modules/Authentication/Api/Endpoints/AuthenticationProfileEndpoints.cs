using Microsoft.AspNetCore.Http.HttpResults;
using Vitarish.Authentication.Contracts.Responses;
using Vitarish.Authentication.Contracts.Routes;
using Vitarish.Authentication.Domain.Security;
using Vitarish.Modules.Base.Core.Endpoints;

namespace Vitarish.Authentication.Api.Endpoints;

public static class AuthenticationProfileEndpoints
{
    public static RouteGroupBuilder MapAuthenticationProfileEndpoint(this RouteGroupBuilder group)
    {
        group.MapModuleGet(AuthenticationRoutes.ProfilePath, ([Microsoft.AspNetCore.Mvc.FromServices] AuthenticationProfilePolicy policy) =>
            Results.Ok(new AuthenticationProfileDto(
                policy.AllowPasswordlessPhoneLogin,
                policy.AllowPasswordlessEmailLogin,
                policy.AllowPasskeyLogin)),
            "Authentication_Profile_Get",
            typeof(AuthenticationProfileDto),
            profile: ModuleEndpointProfile.AnonymousJson,
            anonymousJustification: "authentication.profile")
            .Produces<AuthenticationProfileDto>(StatusCodes.Status200OK)
            .WithSummary("Gets the authentication methods enabled by the active deployment profile.");

        return group;
    }
}
