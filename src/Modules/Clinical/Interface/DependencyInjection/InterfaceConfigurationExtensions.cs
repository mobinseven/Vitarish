using Vitarish.Clinical.Interface.Plugins;
using Vitarish.Medical.Interface.Plugins;

namespace Vitarish.Clinical.Interface.DependencyInjection;

public static class InterfaceConfigurationExtensions
{
    public static IServiceCollection ConfigureClinicalInterface(this IServiceCollection services)
    {
        _ = services.AddScoped<IMedicalPatientSectionPlugin, ClinicalPatientSectionPlugin>();
        return services;
    }
}
