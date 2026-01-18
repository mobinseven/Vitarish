using Vitarish.Sea.Interface.Plugins.Clinic.Projects;

namespace Vitarish.Sea.Interface.Plugins.Clinic.Plugins;

internal class ClinicProjectsGridPlugin : IProjectsGridPlugin
{
    public string Id => SeaPluginIdentifiers.Clinic;

    public RenderFragment Render(ProjectsGridPlugin context) => builder =>
    {
        builder.OpenComponent<ClinicProjectsGrid>(0);
        builder.AddAttribute(1, nameof(context.OnClicked), EventCallback.Factory.Create(this, context.OnClicked));
        builder.AddAttribute(2, nameof(context.OnCreated), EventCallback.Factory.Create(this, context.OnCreated));
        builder.CloseComponent();
    };
}
