using Vitarish.Medical.Interface.Components.Patients;
using Vitarish.Sea.Interface.Plugins;

namespace Vitarish.Medical.Interface.Plugins;

internal class PatientsGridPlugin : IProjectsGridPlugin
{
    public string Id => Sea.Interface.Plugins.PluginIdentifiers.Clinic;

    public RenderFragment Render(ProjectsGridPlugin context) => builder =>
    {
        builder.OpenComponent<PatientsGrid>(0);
        builder.AddAttribute(1, nameof(context.OnClicked), EventCallback.Factory.Create(this, context.OnClicked));
        builder.AddAttribute(2, nameof(context.OnAdd), EventCallback.Factory.Create(this, context.OnAdd));
        builder.AddAttribute(3, nameof(context.TemplateMode), context.TemplateMode);
        builder.CloseComponent();
    };
}
