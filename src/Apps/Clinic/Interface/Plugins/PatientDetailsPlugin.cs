using Vitarish.Clinic.Interface.Components.Patients;
using Vitarish.Sea.Interface.Plugins;

namespace Vitarish.Clinic.Interface.Plugins;

internal class PatientDetailsPlugin : IProjectDetailsPlugin
{
    public string Id => Sea.Interface.Plugins.PluginIdentifiers.Clinic;

    public RenderFragment Render(ProjectDetailsPlugin context) => builder =>
    {
        builder.OpenComponent<PatientDetails>(0);
        builder.AddAttribute(1, nameof(context.Project), context.Project);
        builder.AddAttribute(2, nameof(context.OnEdit), EventCallback.Factory.Create(this, context.OnEdit));
        builder.AddAttribute(3, nameof(context.OnDeleted), EventCallback.Factory.Create(this, context.OnDeleted));
        builder.CloseComponent();
    };
}
