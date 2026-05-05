using Vitarish.Clinical.Domain.Abstractions;
using Vitarish.Clinical.Infrastructure.Data;
using Vitarish.Clinical.Infrastructure.Services;

namespace Vitarish.Clinical.Infrastructure.DependencyInjection;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddClinicalInfrastructure(this IServiceCollection services)
    {
        _ = services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ClinicalDbContext).Assembly));
        _ = services.AddScoped<IFhirGateway, FhirGateway>();
        _ = services.AddScoped<IClinicalConnectorHealthService, ClinicalConnectorHealthService>();
        return services;
    }
}
