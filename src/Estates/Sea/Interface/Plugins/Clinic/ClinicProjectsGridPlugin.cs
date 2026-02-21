using Vitarish.Sea.Interface.Plugins.Clinic.Components;

namespace Vitarish.Sea.Interface.Plugins.Clinic;

internal class ClinicProjectsGridPlugin : IProjectsGridPlugin
{
    public string Id => PluginIdentifiers.Clinic;

    public RenderFragment Render(ProjectsGridPlugin context) => builder =>
    {
        builder.OpenComponent<ClinicProjectsGrid>(0);
        builder.AddAttribute(1, nameof(context.OnClicked), EventCallback.Factory.Create(this, context.OnClicked));
        builder.AddAttribute(2, nameof(context.OnAdd), EventCallback.Factory.Create(this, context.OnAdd));
        builder.AddAttribute(3, nameof(context.TemplateMode), context.TemplateMode);
        builder.CloseComponent();
    };
}
