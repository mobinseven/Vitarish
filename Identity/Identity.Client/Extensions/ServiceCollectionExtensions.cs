using Microsoft.Extensions.DependencyInjection;
using Vitarish.Identity.Client.Services;

namespace Vitarish.Identity.Client.Extensions;

/// <summary>
/// Extension methods for registering Identity client services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Identity API client to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="baseUrl">The base URL of the Identity API.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddIdentityApiClient(this IServiceCollection services, string baseUrl)
    {
        services.AddHttpClient<IdentityApiClient>(client =>
        {
            client.BaseAddress = new Uri(baseUrl);
        });

        return services;
    }

    /// <summary>
    /// Adds the Identity API client to the service collection with a custom configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureClient">Action to configure the HTTP client.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddIdentityApiClient(this IServiceCollection services, Action<HttpClient> configureClient)
    {
        services.AddHttpClient<IdentityApiClient>(configureClient);

        return services;
    }
}
