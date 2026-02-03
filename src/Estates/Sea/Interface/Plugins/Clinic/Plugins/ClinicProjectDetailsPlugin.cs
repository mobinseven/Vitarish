using Vitarish.Sea.Interface.Plugins.Clinic.Projects;

namespace Vitarish.Sea.Interface.Plugins.Clinic.Plugins;

internal class ClinicProjectDetailsPlugin : IProjectDetailsPlugin
{
    public string Id => SeaPluginIdentifiers.Clinic;

    public RenderFragment Render(ProjectDetailsPlugin context) => builder =>
    {
        builder.OpenComponent<ClinicProjectDetails>(0);
        builder.AddAttribute(1, nameof(context.Project), context.Project);
        builder.AddAttribute(2, nameof(context.OnEdit), EventCallback.Factory.Create(this, context.OnEdit));
        builder.AddAttribute(3, nameof(context.OnDeleted), EventCallback.Factory.Create(this, context.OnDeleted));
        builder.CloseComponent();
    };
}
