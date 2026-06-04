using Vitarish.Files.Domain.Options;
using Vitarish.Interfaces.Accounts.Services;
using Vitarish.Search.Domain.Options;

namespace Vitarish.Main.Infrastructure.Options;

/// <summary>
/// Extension methods for configuring application options in the application.
/// </summary>
public static class ApplicationOptionsExtensions
{
    /// <summary>
    /// Adds and configures all application options to the service collection.
    /// This binds the "Vorn" configuration section and registers the aggregated
    /// <see cref="ApplicationOptions"/> with the DI container using the options pattern.
    /// </summary>
    /// <param name="builder">The web application builder.</param>
    /// <returns>The web application builder for chaining.</returns>
    public static WebApplicationBuilder AddApplicationOptions(this WebApplicationBuilder builder)
    {
        _ = builder.Services.AddApplicationOptions(builder.Configuration);
        return builder;
    }

    /// <summary>
    /// Adds and configures all application options to the service collection.
    /// This binds the "Vorn" configuration section and registers the aggregated
    /// <see cref="ApplicationOptions"/> with the DI container using the options pattern.
    /// Also registers module-specific option types for consumption by each module.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration root.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddApplicationOptions(this IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        _ = services.AddOptions<ApplicationOptions>()
            .BindConfiguration(ApplicationOptions.Section)
            .ValidateDataAnnotations()
            .Validate(o => !string.IsNullOrEmpty(o.Minio.Endpoint),
                     $"{ApplicationOptions.Section}:{nameof(ApplicationOptions.Minio)}:{nameof(ApplicationOptions.Minio.Endpoint)} is not defined")
            .ValidateOnStart();

        // Register the concrete ApplicationOptions type as singleton
        //!string.IsNullOrEmpty(o.Files.Minio.Endpoint), 
        _ = services.AddSingleton(sp =>
        {
            IOptions<ApplicationOptions> options = sp.GetRequiredService<IOptions<ApplicationOptions>>();
            return options.Value;
        });

        // Register module-specific option types for consumption by each module
        // Intelligence module options
        _ = services.AddOptions<Intelligence.Domain.Options.AiOptions>()
            .BindConfiguration($"{ApplicationOptions.Section}:Ai");

        _ = services.AddOptions<MinioOptions>()
            .BindConfiguration($"{ApplicationOptions.Section}:Minio");

        // Communications module options
        _ = services.AddOptions<Communications.Domain.Options.ProvidersOptions>()
            .BindConfiguration($"{ApplicationOptions.Section}:Providers");

        _ = services.AddOptions<Communications.Domain.Options.NotificationOptions>()
            .BindConfiguration($"{ApplicationOptions.Section}:Notifications");

        _ = services.AddOptions<ElasticSearchOptions>()
            .BindConfiguration($"{ApplicationOptions.Section}:ElasticSearch");

        _ = services.AddOptions<FeaturesOptions>()
            .BindConfiguration($"{ApplicationOptions.Section}:Features");

        _ = services.AddOptions<VerificationSmsShieldOptions>()
            .BindConfiguration(VerificationSmsShieldOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }
}
