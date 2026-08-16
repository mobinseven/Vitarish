using Microsoft.AspNetCore.Authorization;
using Vitarish.Modules.Base.Core.Endpoints;
using Vitarish.Main.Authorization;

namespace Vitarish.Main.DependencyInjection;

public static class HostAuthorizationExtenstions
{
    public static void AddHostAuthorization(this WebApplicationBuilder builder)
    {
        _ = builder.Services.AddAuthorization(options =>
        {
            // Any endpoint that is not explicitly public or protected by a
            // narrower policy is denied by default. This keeps new routes
            // from becoming anonymous through missing metadata.
            options.FallbackPolicy = new AuthorizationPolicyBuilder(
                    ModuleEndpointAuthenticationSchemes.Bearer,
                    ModuleEndpointAuthenticationSchemes.Browser)
                .RequireAuthenticatedUser()
                .Build();
        });
        _ = builder.Services.AddScoped<IAuthorizationHandler, Authorizer>();
        _ = builder.Services.AddSingleton<IAuthorizationPolicyProvider, AuthorizerPolicyProvider>();

    }
}
