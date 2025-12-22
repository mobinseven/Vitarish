using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Clients.Identity.Abstractions;
using Vitarish.Clients.Identity.Internal;
using Vitarish.Clients.Identity.Options;

public static class IdentityClientMarkerExtensions
{
    private const string ConfigSection = $"{nameof(Vitarish)}:Identity";

    /// <summary>
    /// This method adds the AAAS API client services to the service collection, enabling communication with the AAAS for authentication and authorization purposes plus communicating authorized service providers.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static IServiceCollection AddIdentityClient(this IServiceCollection services, IConfiguration configuration)
    {
        if (services.Any(sd => sd.ServiceType == typeof(ClientsIdentityMarker)))
        {
            return services;
        }

        _ = services.AddSingleton<ClientsIdentityMarker>();

        _ = services.AddOptions<IdentityClientOptions>()
                .Bind(configuration.GetSection(ConfigSection))
                .Validate(o => !string.IsNullOrWhiteSpace(o.Authority) &&
                               !string.IsNullOrWhiteSpace(o.ClientId) &&
                               !string.IsNullOrWhiteSpace(o.Secret),
                          $"{ConfigSection}:* must be configured")
                .ValidateOnStart();

        services.AddRedisCaching(configuration);

        //services.AddOpenTelemetry()
        //        .WithTracing(b => b
        //            .AddAspNetCoreInstrumentation()
        //            .AddHttpClientInstrumentation());

        _ = services.AddMemoryCache();
        _ = services.AddHybridCache();

        _ = services.AddTransient<IIdentityClientService, IdentityClientService>();
        _ = services.AddHttpContextAccessor();
        return services;
    }

    public static WebApplicationBuilder AddIdentityClient(this WebApplicationBuilder builder)
    {
        _ = builder.Services.AddIdentityClient(builder.Configuration);
        return builder;
    }

    private sealed class ClientsIdentityMarker { }
}
