using Microsoft.Extensions.DependencyInjection;
using Vitarish.Identity.Core.Abstractions;
using Vitarish.Identity.Infrastructure.Context;

namespace Vitarish.Identity.Api.Extensions;

/// <summary>
/// Extension methods for configuring Identity services.
/// </summary>
public static class IdentityServiceCollectionExtensions
{
    /// <summary>
    /// Adds Identity context services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddIdentityContext(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();

        // Register IIdentityContext as scoped, resolved from HttpContext.User
        services.AddScoped<IIdentityContext>(sp =>
        {
            var httpContextAccessor = sp.GetRequiredService<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
            var user = httpContextAccessor.HttpContext?.User ?? new System.Security.Claims.ClaimsPrincipal();
            return new ClaimsIdentityContext(user);
        });

        // Register IAdminContext as scoped, resolved from HttpContext.User
        services.AddScoped<IAdminContext>(sp =>
        {
            var httpContextAccessor = sp.GetRequiredService<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
            var user = httpContextAccessor.HttpContext?.User ?? new System.Security.Claims.ClaimsPrincipal();
            return new ClaimsAdminContext(user);
        });

        return services;
    }
}
