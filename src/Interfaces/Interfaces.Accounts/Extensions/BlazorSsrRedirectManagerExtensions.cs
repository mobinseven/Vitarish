using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Interfaces.Accounts.Services;

namespace Vitarish.Interfaces.Accounts.Extensions;

public static class BlazorSsrRedirectManagerExtensions
{
    public static IServiceCollection AddBlazorSsrRedirectManager(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        _ = services.AddScoped<IdentityRedirectManager>();
        return services;
    }

    public static void RedirectTo(this HttpContext httpContext, string redirectionUrl)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        httpContext.Response.Headers.Append("blazor-enhanced-nav-redirect-location", redirectionUrl);
        httpContext.Response.StatusCode = 200;
    }
}
