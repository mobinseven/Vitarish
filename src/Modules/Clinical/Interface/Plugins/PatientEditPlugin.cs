using Vitarish.Clinical.Interface.Components.Patients;
using Vitarish.Sea.Interface.Plugins;

namespace Vitarish.Clinical.Interface.Plugins;

internal class PatientEditPlugin : IProjectEditPlugin
{
    public string Id => Sea.Interface.Plugins.PluginIdentifiers.Clinic;

    public RenderFragment Render(ProjectEditPlugin context) => builder =>
    {
        builder.OpenComponent<PatientEditForm>(0);
        builder.AddAttribute(1, nameof(context.Project), context.Project);
        builder.AddAttribute(2, nameof(context.OnUpdate), EventCallback.Factory.Create(this, context.OnUpdate));
        builder.AddAttribute(3, nameof(context.OnCreate), EventCallback.Factory.Create(this, context.OnCreate));
        builder.AddAttribute(4, nameof(context.OnCancel), EventCallback.Factory.Create(this, context.OnCancel));
        builder.AddAttribute(5, nameof(context.TemplateMode), context.TemplateMode);
        builder.CloseComponent();
    };
}
