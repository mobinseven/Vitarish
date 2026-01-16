namespace Vitarish.Sea.Interface.Components.Projects.Plugins;

internal sealed record ProjectsGridPlugin(
    EventCallback<Guid> OnClicked,
    EventCallback<Guid> OnCreated) : PluginContext()
{
}
internal interface IProjectsGridPlugin : IPlugin<ProjectsGridPlugin>
{
}
