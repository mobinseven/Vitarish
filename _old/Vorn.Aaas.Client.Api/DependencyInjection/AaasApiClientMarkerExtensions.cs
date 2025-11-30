using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Vorn.Aaas.Client.Api.Abstractions;
using Vorn.Aaas.Client.Api.Internal;
using Vorn.Aaas.Client.Api.Options;

public static class AaasApiClientMarkerExtensions
{
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
        IConfigurationSection section = configuration.GetSection(VornOptions.Section);
        _ = services.Configure<VornOptions>(section);

        _ = services.AddOptions<VornOptions>()
                .BindConfiguration(VornOptions.Section)
                .Validate(o => !string.IsNullOrWhiteSpace(o.Aaas.Authority),
                          "Vorn:Aaas:Authority must be configured")
                .ValidateOnStart();
        _ = services.AddAuthentication()
                .AddJwtBearer(jwt =>
                {
                    IOptions<VornOptions> options = services.BuildServiceProvider().GetRequiredService<IOptions<VornOptions>>();
                    jwt.Authority = options.Value.Aaas.Authority;
                    jwt.RequireHttpsMetadata = true;
                    jwt.TokenValidationParameters.ValidateAudience = false;
                    jwt.TokenValidationParameters.ValidTypes = ["at+jwt"];
                });
        await services.AddAaasDataProtection(configuration);
        services.AddRedisCaching(configuration);
        _ = services.AddAuthorization();
        _ = services.AddMemoryCache();
        _ = services.AddTransient<IAaasClientFactory, AaasClientFactory>();
        _ = services.AddScoped<IRequestIdentityForwarder, RequestIdentityForwarder>();
        _ = services.AddScoped<RequestIdentityContext>();
        _ = services.AddScoped<IRequestIdentityContext>(sp => sp.GetRequiredService<RequestIdentityContext>());
        _ = services.AddScoped<IRequestIdentityContextWriter>(sp => sp.GetRequiredService<RequestIdentityContext>());
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

        _ = services.AddOptions<VornOptions>()
                .Bind(configuration.GetSection(VornOptions.Section))
                .Validate(o => !string.IsNullOrWhiteSpace(o.Aaas.Authority) &&
                               !string.IsNullOrWhiteSpace(o.Aaas.ClientId) &&
                               !string.IsNullOrWhiteSpace(o.Aaas.Secret),
                          "Vorn:Aaas:* must be configured")
                .ValidateOnStart();

        VornOptions vornOptions = services.BuildServiceProvider().GetRequiredService<IOptions<VornOptions>>().Value;

        services.AddRedisCaching(configuration);

        //services.AddOpenTelemetry()
        //        .WithTracing(b => b
        //            .AddAspNetCoreInstrumentation()
        //            .AddHttpClientInstrumentation());

        _ = services.AddMemoryCache();
        _ = services.AddScoped<IRequestIdentityForwarder, RequestIdentityForwarder>();
        _ = services.AddScoped<RequestIdentityContext>();
        _ = services.AddScoped<IRequestIdentityContext>(sp => sp.GetRequiredService<RequestIdentityContext>());
        _ = services.AddScoped<IRequestIdentityContextWriter>(sp => sp.GetRequiredService<RequestIdentityContext>());
        _ = services.AddScoped<IRequestIdentityContextGenerator, RequestIdentityContextGenerator>();
        _ = services.AddSingleton<IIdentityScopeFactory, IdentityScopeFactory>();
        _ = services.AddTransient<IApiClientService, ApiClientService>();
        _ = services.AddTransient<IAaasClientFactory, AaasClientFactory>();
        _ = services.AddScoped<ITenantsService, TenantsService>();
        _ = services.AddHttpContextAccessor();
        return services;
    }

    public static WebApplicationBuilder AddAaasApiClient(this WebApplicationBuilder builder)
    {
        _ = builder.Services.AddAaasApiClient(builder.Configuration);
        return builder;
    }

    private sealed class AaasApiClientMarker { }
}
