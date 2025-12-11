
using Microsoft.AspNetCore.Authorization;

public static class EndpointAuthorizationExtensions
{
    public static void AddEndpointAuthorization(this IEndpointConventionBuilder builder, string scheme = "IdentityServerAccessToken") => builder
                .RequireAuthorization(new AuthorizeAttribute
                {
                    AuthenticationSchemes = scheme
                });
}
