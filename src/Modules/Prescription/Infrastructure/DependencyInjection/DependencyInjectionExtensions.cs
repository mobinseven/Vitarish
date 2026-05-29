using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Prescription.Application.Abstractions;
using Vitarish.Prescription.Infrastructure.Data;
using Vitarish.Prescription.Infrastructure.Providers;
using Vitarish.Prescription.Infrastructure.Providers.Tamin;

namespace Vitarish.Prescription.Infrastructure.DependencyInjection;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddPrescriptionInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        // Core infrastructure
        _ = services.AddScoped<IInsuranceProviderRegistry, InsuranceProviderRegistry>();
        _ = services.AddSingleton<IProviderResiliencePipelineProvider>(
            sp => new ConfigurationResiliencePipelineProvider(
                sp.GetRequiredService<IConfiguration>()));

        // MediatR handlers
        _ = services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(
            typeof(PrescriptionDbContext).Assembly));

        // Tamin provider
        RegisterTaminProvider(services, configuration);

        return services;
    }

    private static void RegisterTaminProvider(IServiceCollection services, IConfiguration configuration)
    {
        _ = services.Configure<TaminOptions>(configuration.GetSection(TaminOptions.SectionName));
        _ = services.AddHttpClient<TaminInsuranceAdapter>();
        _ = services.AddScoped<IInsuranceProviderAdapter, TaminInsuranceAdapter>();
        _ = services.AddSingleton<IProviderErrorMapper, TaminErrorMapper>();
    }
}
