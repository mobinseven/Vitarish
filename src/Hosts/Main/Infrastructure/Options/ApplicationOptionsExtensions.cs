using Vitarish.Interfaces.Accounts.Services;

namespace Vitarish.Main.Infrastructure.Options;

/// <summary>
/// Extension methods for configuring application options in the application.
/// </summary>
public static class ApplicationOptionsExtensions
{
    /// <summary>
    /// Adds and configures all application options to the service collection.
    /// </summary>
    /// <param name="builder">The web application builder.</param>
    /// <returns>The web application builder for chaining.</returns>
    public static WebApplicationBuilder AddApplicationOptions(this WebApplicationBuilder builder)
    {
        _ = builder.Services.AddApplicationOptions(builder.Configuration);
        return builder;
    }

    /// <summary>
    /// Adds and configures host-owned application options.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration root.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddApplicationOptions(
        this IServiceCollection services,
        Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        _ = services.AddOptions<ApplicationOptions>()
            .BindConfiguration(ApplicationOptions.Section)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        _ = services.AddSingleton(sp =>
        {
            IOptions<ApplicationOptions> options = sp.GetRequiredService<IOptions<ApplicationOptions>>();
            return options.Value;
        });

        _ = services.AddOptions<FeaturesOptions>()
            .BindConfiguration($"{ApplicationOptions.Section}:Features");

        _ = services.AddOptions<VerificationSmsShieldOptions>()
            .BindConfiguration(VerificationSmsShieldOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }
}
