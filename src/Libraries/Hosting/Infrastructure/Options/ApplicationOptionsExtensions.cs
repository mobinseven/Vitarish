namespace Vitarish.Main.Infrastructure.Options;

public static class ApplicationOptionsExtensions
{
    public static WebApplicationBuilder AddApplicationOptions(this WebApplicationBuilder builder)
    {
        _ = builder.Services.AddApplicationOptions(builder.Configuration);
        return builder;
    }

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

        _ = services.AddOptions<CorsOptions>()
            .BindConfiguration(CorsOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }
}

public sealed class VerificationSmsShieldOptions
{
    public const string SectionName = "VerificationSmsShield";
    public int MaxAttempts { get; set; } = 5;
    public int WindowSeconds { get; set; } = 300;
}

