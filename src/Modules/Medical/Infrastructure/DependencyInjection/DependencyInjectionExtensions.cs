using Microsoft.Extensions.Configuration;
using Vitarish.Medical.Application.Abstractions.Prescriptions;
using Vitarish.Medical.Infrastructure.Providers.Prescriptions;
using Vitarish.Medical.Infrastructure.Providers.Prescriptions.Salamat;
using Vitarish.Medical.Infrastructure.Providers.Prescriptions.Tamin;
using Vitarish.Medical.Infrastructure.Services.Prescriptions;
using Vitarish.Modules.Base.Core.Platform.Transactions;
using Vitarish.Modules.Base.Core.Platform.Retry;

namespace Vitarish.Medical.Infrastructure.DependencyInjection;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddMedicalInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        _ = services.AddVitarishMedicalInfrastructure();

        _ = services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(
            typeof(MedicalDbContext).Assembly));

        // Prescription infrastructure
        AddPrescriptionInfrastructure(services, configuration);

        _ = services.AddMedicalBackgroundJobs();

        return services;
    }

    private static void AddPrescriptionInfrastructure(IServiceCollection services, IConfiguration configuration)
    {
        // Core infrastructure
        _ = services.AddScoped<IInsuranceProviderRegistry, InsuranceProviderRegistry>();
        _ = services.AddSingleton<IProviderResiliencePipelineProvider>(
            sp => new ConfigurationResiliencePipelineProvider(
                sp.GetRequiredService<IConfiguration>()));

        // Outbox service for retry/recovery
        _ = services.AddScoped<IOutboxService, PrescriptionOutboxService>();

        // Tamin provider
        RegisterTaminProvider(services, configuration);

        // Salamat provider
        RegisterSalamatProvider(services, configuration);
    }

    private static void RegisterTaminProvider(IServiceCollection services, IConfiguration configuration)
    {
        _ = services.Configure<TaminOptions>(configuration.GetSection(TaminOptions.SectionName));
        _ = services.AddHttpClient("TaminClient");
        _ = services.AddSingleton<ITaminDateFormatter, TaminDateFormatter>();
        _ = services.AddScoped<ITaminSessionFactory, TaminSessionFactory>();
        _ = services.AddScoped<ITaminMedicalInfrastructureGateway, TaminMedicalInfrastructureGateway>();
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
