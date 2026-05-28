using Microsoft.Extensions.DependencyInjection;
using Vitarish.Prescription.Application.Abstractions;
using Vitarish.Prescription.Infrastructure.Data;
using Vitarish.Prescription.Infrastructure.Providers;

namespace Vitarish.Prescription.Infrastructure.DependencyInjection;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddPrescriptionInfrastructure(this IServiceCollection services)
    {
        _ = services.AddSingleton<IInsuranceProviderRegistry, InsuranceProviderRegistry>();

        _ = services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(
            typeof(PrescriptionDbContext).Assembly));

        return services;
    }
}
