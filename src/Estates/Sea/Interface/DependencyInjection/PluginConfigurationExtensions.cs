using Vitarish.Sea.Interface.Plugins.Clinic;

namespace Vitarish.Sea.Interface.DependencyInjection;

public static class PluginConfigurationExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection ConfigurePlugins()
        {
            _ = services.AddScoped<IProjectsGridPlugin, ClinicProjectsGridPlugin>();
            _ = services.AddScoped<IProjectEditPlugin, ClinicProjectEditPlugin>();
            _ = services.AddScoped<IProjectDetailsPlugin, ClinicProjectDetailsPlugin>();
            _ = services.AddScoped<IEventItemTimelinePlugin, AttachmentPlugin>();
            _ = services.AddScoped<IEventItemCreationPlugin, AttachmentPlugin>();
            _ = services.AddScoped<IEventItemTimelinePlugin, ReminderPlugin>();
            _ = services.AddScoped<IEventItemCreationPlugin, ReminderPlugin>();
            return services;
        }
    }
}
