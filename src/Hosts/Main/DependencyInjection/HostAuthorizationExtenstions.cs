using Microsoft.AspNetCore.Authorization;
using Vitarish.Main.Services;

namespace Vitarish.Main.DependencyInjection;

public static class HostAuthorizationExtenstions
{
    public static void AddHostAuthorization(this WebApplicationBuilder builder)
    {
        _ = builder.Services.AddAuthorization();
        _ = builder.Services.AddScoped<IAuthorizationHandler, Authorizer>();
        _ = builder.Services.AddSingleton<IAuthorizationPolicyProvider, AuthorizerPolicyProvider>();
    }
}
