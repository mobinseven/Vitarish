using Vitarish.Medical.Interface.Plugins;
using Vitarish.Sea.Interface.Plugins;

namespace Vitarish.Medical.Interface.DependencyInjection;

public static class PluginConfigurationExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection ConfigureMedicalPlugins()
        {
            _ = services.AddScoped<IProjectsGridPlugin, PatientsGridPlugin>();
            _ = services.AddScoped<IProjectEditPlugin, PatientEditPlugin>();
            _ = services.AddScoped<IProjectDetailsPlugin, PatientDetailsPlugin>();
            return services;
        }
    }
}
