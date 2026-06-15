using Vitarish.Interfaces.Accounts.Services;
using Vitarish.Modules.Base;

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
        IReadOnlyList<IModule> modules = builder.Services
            .Where(descriptor => descriptor.ServiceType == typeof(IReadOnlyList<IModule>))
            .Select(descriptor => descriptor.ImplementationInstance)
            .OfType<IReadOnlyList<IModule>>()
            .LastOrDefault() ?? [];
        _ = builder.Services.AddApplicationOptions(builder.Configuration, modules);
        return builder;
    }

    /// <summary>
    /// Adds and configures host-owned application options and delegates module-owned option registration to modules.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration root.</param>
    /// <param name="modules">The modules that own module-specific option binding.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddApplicationOptions(
        this IServiceCollection services,
        Microsoft.Extensions.Configuration.IConfiguration configuration,
        IReadOnlyList<IModule>? modules = null)
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

        foreach (IModule module in modules ?? [])
        {
            string moduleSectionPath = $"{ApplicationOptions.Section}:{module.Name}";
            module.RegisterOptions(services, configuration, moduleSectionPath);
        }

        _ = services.AddOptions<FeaturesOptions>()
            .BindConfiguration($"{ApplicationOptions.Section}:Features");

        _ = services.AddOptions<VerificationSmsShieldOptions>()
            .BindConfiguration(VerificationSmsShieldOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }
}
