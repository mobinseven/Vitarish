namespace Vitarish.Sea.Interface.Plugins.Clinic.Projects;

internal class ClinicProjectsGridPlugin : IProjectsGridPlugin
{
    public string Id => SeaPluginIdentifiers.Clinic;

    public string DisplayName => nameof(ClinicProjectsGridPlugin);

    public string? Description => nameof(ClinicProjectsGridPlugin);

    public bool IsAvailable(ProjectsGridPlugin context) => true;
    public RenderFragment Render(ProjectsGridPlugin context) => builder =>
    {
        builder.OpenComponent<ClinicProjectsGrid>(0);
        builder.AddAttribute(1, nameof(context.OnClicked), EventCallback.Factory.Create(this, context.OnClicked));
        builder.AddAttribute(2, nameof(context.OnCreated), EventCallback.Factory.Create(this, context.OnCreated));
        builder.CloseComponent();
    };
}
