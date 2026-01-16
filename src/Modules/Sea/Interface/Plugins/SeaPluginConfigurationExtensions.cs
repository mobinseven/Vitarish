using Vitarish.Sea.Interface.Components.Attachments;
using Vitarish.Sea.Interface.Components.Reminders;
using Vitarish.Sea.Interface.Plugins.Clinic.Projects;

namespace Vitarish.Sea.Interface.Plugins;

public static class SeaPluginConfigurationExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection ConfigureSeaPlugins()
        {
            _ = services.AddScoped<IProjectsGridPlugin, ClinicProjectsGridPlugin>();
            _ = services.AddScoped<IEventItemTimelinePlugin, AttachmentPlugin>();
            _ = services.AddScoped<IEventItemCreationPlugin, AttachmentPlugin>();
            _ = services.AddScoped<IEventItemTimelinePlugin, ReminderPlugin>();
            _ = services.AddScoped<IEventItemCreationPlugin, ReminderPlugin>();
            // _ = services.AddScoped<IProjectsGridPlugin, GymProjectsGridPlugin>();
            return services;
        }
    }
}
