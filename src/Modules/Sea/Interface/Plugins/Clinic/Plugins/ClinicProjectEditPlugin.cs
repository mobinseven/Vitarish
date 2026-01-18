using Vitarish.Sea.Interface.Plugins.Clinic.Projects;

namespace Vitarish.Sea.Interface.Plugins.Clinic.Plugins;

internal class ClinicProjectEditPlugin : IProjectEditPlugin
{
    public string Id => SeaPluginIdentifiers.Clinic;
    public RenderFragment Render(ProjectEditPlugin context) => builder =>
    {
        builder.OpenComponent<ClinicProjectEditForm>(0);
        builder.AddAttribute(1, nameof(context.Project), context.Project);
        builder.AddAttribute(2, nameof(context.OnUpdate), EventCallback.Factory.Create(this, context.OnUpdate));
        builder.CloseComponent();
    };
}
