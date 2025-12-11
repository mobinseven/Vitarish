using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Vitarish.Clients.Gateway.Abstractions;
using Vitarish.Clients.Gateway.Internal;
using Vitarish.Clients.Gateway.Options;

public static class AaasApiClientMarkerExtensions
{
    private const string ConfigSection = "Vorn";

    /// <summary>
    /// This method configures the necessary services for integrating with the AAAS (Authentication as a Service) as a service provider.
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static async Task ConfigureAaasClient(this IServiceCollection services, IConfiguration configuration)
    {
        if (services.Any(sd => sd.ServiceType == typeof(AaasApiClientMarker)))
        {
            return;
        }

        _ = services.AddSingleton<AaasApiClientMarker>();
        IConfigurationSection section = configuration.GetSection($"{ConfigSection}:Aaas");
        _ = services.Configure<IdentityOptions>(section);

        _ = services.AddOptions<IdentityOptions>()
                .BindConfiguration($"{ConfigSection}:Aaas")
                .Validate(o => !string.IsNullOrWhiteSpace(o.Authority),
                          "Vorn:Aaas:Authority must be configured")
                .ValidateOnStart();
        _ = services.AddAuthentication()
                .AddJwtBearer(jwt =>
                {
                    IOptions<IdentityOptions> options = services.BuildServiceProvider().GetRequiredService<IOptions<IdentityOptions>>();
                    jwt.Authority = options.Value.Authority;
                    jwt.RequireHttpsMetadata = true;
                    jwt.TokenValidationParameters.ValidateAudience = false;
                    jwt.TokenValidationParameters.ValidTypes = ["at+jwt"];
                });
        await services.AddAaasDataProtection(configuration);
        services.AddRedisCaching(configuration);
        _ = services.AddAuthorization();
        _ = services.AddMemoryCache();
        _ = services.AddTransient<IIdentityClientFactory, IdentityClientFactory>();
        _ = services.AddHttpContextAccessor();
        _ = services.AddScoped<IClientIdentityContext, ClientIdentityContext>();
    }

    /// <summary>
    /// This method adds the AAAS API client services to the service collection, enabling communication with the AAAS for authentication and authorization purposes plus communicating authorized service providers.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static IServiceCollection AddAaasApiClient(this IServiceCollection services, IConfiguration configuration)
    {
        if (services.Any(sd => sd.ServiceType == typeof(AaasApiClientMarker)))
        {
            return services;
        }

        _ = services.AddSingleton<AaasApiClientMarker>();

        _ = services.AddOptions<IdentityOptions>()
                .Bind(configuration.GetSection($"{ConfigSection}:Aaas"))
                .Validate(o => !string.IsNullOrWhiteSpace(o.Authority) &&
                               !string.IsNullOrWhiteSpace(o.ClientId) &&
                               !string.IsNullOrWhiteSpace(o.Secret),
                          "Vorn:Aaas:* must be configured")
                .ValidateOnStart();

        services.AddRedisCaching(configuration);

        //services.AddOpenTelemetry()
        //        .WithTracing(b => b
        //            .AddAspNetCoreInstrumentation()
        //            .AddHttpClientInstrumentation());

        _ = services.AddMemoryCache();
        _ = services.AddTransient<IApiClientService, ApiClientService>();
        _ = services.AddTransient<IIdentityClientFactory, IdentityClientFactory>();
        _ = services.AddScoped<ITenantsService, TenantsService>();
        _ = services.AddHttpContextAccessor();
        _ = services.AddScoped<IClientIdentityContext, ClientIdentityContext>();
        return services;
    }

    public static WebApplicationBuilder AddAaasApiClient(this WebApplicationBuilder builder)
    {
        _ = builder.Services.AddAaasApiClient(builder.Configuration);
        return builder;
    }

    private sealed class AaasApiClientMarker { }
}
