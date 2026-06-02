using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Vitarish.Modules.Base.Platform.Retry;
using Vitarish.Modules.Base.Platform.Transactions;
using Vitarish.Prescription.Application.Abstractions;
using Vitarish.Prescription.Infrastructure.Data;
using Vitarish.Prescription.Infrastructure.Providers;
using Vitarish.Prescription.Infrastructure.Providers.Salamat;
using Vitarish.Prescription.Infrastructure.Providers.Tamin;
using Vitarish.Prescription.Infrastructure.Services;

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

        // Outbox service for retry/recovery
        _ = services.AddScoped<IOutboxService, PrescriptionOutboxService>();

        // MediatR handlers
        _ = services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(
            typeof(PrescriptionDbContext).Assembly));

        // Tamin provider
        RegisterTaminProvider(services, configuration);

        // Salamat provider
        RegisterSalamatProvider(services, configuration);

        return services;
    }

    private static void RegisterTaminProvider(IServiceCollection services, IConfiguration configuration)
    {
        _ = services.Configure<TaminOptions>(configuration.GetSection(TaminOptions.SectionName));
        _ = services.AddHttpClient("TaminClient");
        _ = services.AddScoped<ITaminCredentialSessionStore, TaminCredentialSessionStore>();
        _ = services.AddScoped<TaminInsuranceAdapter>();
        _ = services.AddSingleton<TaminErrorMapper>();
        _ = services.AddSingleton<IProviderErrorMapper>(sp => sp.GetRequiredService<TaminErrorMapper>());
        _ = services.AddScoped<IInsuranceProviderAdapter>(sp =>
            new ResilientAdapterDecorator(
                sp.GetRequiredService<TaminInsuranceAdapter>(),
                sp.GetRequiredService<TaminErrorMapper>(),
                sp.GetRequiredService<IProviderResiliencePipelineProvider>(),
                sp.GetRequiredService<IExternalApiTransactionLogger>(),
                sp.GetRequiredService<ILogger<ResilientAdapterDecorator>>()));
    }

    private static void RegisterSalamatProvider(IServiceCollection services, IConfiguration configuration)
    {
        _ = services.Configure<SalamatOptions>(configuration.GetSection(SalamatOptions.SectionName));
        _ = services.AddHttpClient<SalamatInsuranceAdapter>();
        _ = services.AddSingleton<SalamatErrorMapper>();
        _ = services.AddSingleton<IProviderErrorMapper>(sp => sp.GetRequiredService<SalamatErrorMapper>());
        _ = services.AddScoped<IInsuranceProviderAdapter>(sp =>
            new ResilientAdapterDecorator(
                sp.GetRequiredService<SalamatInsuranceAdapter>(),
                sp.GetRequiredService<SalamatErrorMapper>(),
                sp.GetRequiredService<IProviderResiliencePipelineProvider>(),
                sp.GetRequiredService<IExternalApiTransactionLogger>(),
                sp.GetRequiredService<ILogger<ResilientAdapterDecorator>>()));
    }
}
