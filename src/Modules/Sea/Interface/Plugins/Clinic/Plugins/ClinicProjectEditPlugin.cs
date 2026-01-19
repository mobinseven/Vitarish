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
        builder.AddAttribute(3, nameof(context.OnCreate), EventCallback.Factory.Create(this, context.OnCreate));
        builder.AddAttribute(4, nameof(context.OnCancel), EventCallback.Factory.Create(this, context.OnCancel));
        builder.AddAttribute(5, nameof(context.TemplateMode), context.TemplateMode);
        builder.CloseComponent();
    };
}
